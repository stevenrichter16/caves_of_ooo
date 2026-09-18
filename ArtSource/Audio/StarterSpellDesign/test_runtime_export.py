"""Export invariants: preserve approval, smooth retiming, and reject bad inputs."""
import tempfile
import unittest
import wave
from pathlib import Path

import numpy as np

import runtime_export as export


class RuntimeExportTests(unittest.TestCase):
    def signal(self):
        x = np.rint(np.sin(np.arange(12000) * .027) * 2000000).astype(np.int32)
        x[:20] = x[-20:] = 0
        return x

    def join(self, before, after, count, contact):
        y = np.zeros(count, np.int64)
        if before is not None:
            y[:len(before)] += before
        if after is not None:
            y[contact:contact + len(after)] += after
        return y

    def test_neutral_split_reconstructs_every_integer_without_changing_source(self):
        x = self.signal(); original = x.copy()
        before, after = export.split_pcm(x, 4000, 960, gated=False)
        np.testing.assert_array_equal(self.join(before, after, len(x), 4000), x)
        np.testing.assert_array_equal(x, original)

    def test_neutral_crossover_has_zero_boundaries_when_played_apart(self):
        before, after = export.split_pcm(self.signal(), 4000, 960, gated=False)
        self.assertEqual((before[0], before[-1], after[0], after[-1]), (0, 0, 0, 0))
        self.assertLess(abs(int(before[-2])), 20)
        self.assertLess(abs(int(after[1])), 20)

    def test_crossover_does_not_mute_or_double_reference_amplitude(self):
        x = np.full(9000, 1234567, np.int32); x[0] = x[-1] = 0
        before, after = export.split_pcm(x, 3000, 960, gated=False)
        self.assertGreater(before[3500], 0); self.assertGreater(after[500], 0)
        np.testing.assert_array_equal(self.join(before, after, len(x), 3000), x)

    def test_gated_onset_remains_entirely_contact_relative(self):
        x = self.signal(); x[:4001] = 0
        before, after = export.split_pcm(x, 4000, 960, gated=True)
        self.assertIsNone(before)
        np.testing.assert_array_equal(after, x[4000:])

    def test_ungated_already_silent_prefix_also_needs_no_clip(self):
        x = self.signal(); x[:4500] = 0
        before, after = export.split_pcm(x, 4000, 960, gated=False)
        self.assertIsNone(before)
        np.testing.assert_array_equal(after, x[4000:])

    def test_gated_precontact_audio_is_rejected_instead_of_leaking_success(self):
        with self.assertRaises(ValueError):
            export.split_pcm(self.signal(), 4000, 960, gated=True)

    def test_gated_nonzero_first_contact_sample_is_rejected_for_click_risk(self):
        x = self.signal(); x[:4000] = 0; x[4000] = 123
        with self.assertRaises(ValueError):
            export.split_pcm(x, 4000, 960, gated=True)

    def test_fully_silent_layer_emits_no_sources(self):
        before, after = export.split_pcm(np.zeros(9000, np.int32), 3000, 960, gated=False)
        self.assertIsNone(before); self.assertIsNone(after)

    def test_no_contact_tail_emits_no_suffix(self):
        x = self.signal(); x[3000:] = 0
        before, after = export.split_pcm(x, 4000, 960, gated=False)
        self.assertIsNotNone(before); self.assertIsNone(after)
        np.testing.assert_array_equal(self.join(before, after, len(x), 4000), x)

    def test_contact_outside_source_is_rejected(self):
        for contact in [-1, 12000, 12001]:
            with self.subTest(contact=contact), self.assertRaises(ValueError):
                export.split_pcm(self.signal(), contact, 960, gated=False)

    def test_too_short_crossover_is_rejected(self):
        for count in [-1, 0, 1]:
            with self.subTest(count=count), self.assertRaises(ValueError):
                export.split_pcm(self.signal(), 4000, count, gated=False)

    def test_stereo_or_float_input_is_rejected(self):
        for x in [self.signal().astype(float), np.zeros((12000, 2), np.int32)]:
            with self.subTest(shape=x.shape), self.assertRaises(ValueError):
                export.split_pcm(x, 4000, 960, gated=False)

    def test_out_of_pcm24_range_is_rejected(self):
        for value in [-8388609, 8388608]:
            x = self.signal(); x[50] = value
            with self.subTest(value=value), self.assertRaises(ValueError):
                export.split_pcm(x, 4000, 960, gated=False)

    def test_nonquiet_original_boundary_is_rejected(self):
        for index in [0, -1]:
            x = self.signal(); x[index] = 5
            with self.subTest(index=index), self.assertRaises(ValueError):
                export.split_pcm(x, 4000, 960, gated=False)

    def test_pcm24_roundtrip_preserves_signed_extremes(self):
        x = np.array([0, -8388608, -1, 1, 8388607, 0], np.int32)
        with tempfile.TemporaryDirectory() as folder:
            p = Path(folder) / 'exact.wav'
            export.write_pcm24(p, x)
            np.testing.assert_array_equal(export.read_pcm24(p), x)

    def test_wrong_wave_format_is_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            p = Path(folder) / 'wrong.wav'
            with wave.open(str(p), 'wb') as w:
                w.setnchannels(2); w.setsampwidth(2); w.setframerate(44100)
                w.writeframes(bytes(80))
            with self.assertRaises(ValueError):
                export.read_pcm24(p)

    def test_exhaustive_subset_extrema_matches_all_actual_masks(self):
        fixed = np.array([-.1, .4, -.8, 0.0])
        optional = [np.array([.5, -.2, .6, .3]), np.array([-.7, .4, -.1, -.9])]
        peak, index, mask = export.subset_peak(fixed, optional)
        exact = max(float(np.max(np.abs(fixed + sum((x for j, x in enumerate(optional) if m & (1 << j)), np.zeros(4))))) for m in range(4))
        self.assertEqual(peak, exact)
        chosen = fixed + sum((x for j, x in enumerate(optional) if mask & (1 << j)), np.zeros(4))
        self.assertEqual(abs(chosen[index]), peak)

    def test_safe_gain_preserves_balance_and_never_boosts_quiet_sets(self):
        self.assertEqual(export.common_safe_gain(.2), 1)
        gain = export.common_safe_gain(1.5)
        self.assertLess(gain, 1)
        self.assertLessEqual(1.5 * gain, 10 ** (-3.2 / 20))

    def test_nonfinite_or_zero_peak_is_rejected(self):
        for peak in [float('nan'), float('inf'), -1, 0]:
            with self.subTest(peak=peak), self.assertRaises(ValueError):
                export.common_safe_gain(peak)

    def test_approved_manifest_pin_rejects_any_other_hash(self):
        with tempfile.TemporaryDirectory() as folder:
            p = Path(folder) / 'manifest.json'; p.write_text('{}')
            with self.assertRaises(ValueError):
                export.load_approved_manifest(p)

    def test_unknown_gate_mapping_fails_closed(self):
        with self.assertRaises(ValueError):
            export.layer_gate('invented_spell', 'invented_layer')

    def test_any_offset_bound_covers_every_mask_and_unrelated_delay(self):
        prefix = np.array([.3, -.6, .1, .2])
        fixed = np.array([.2, .4, -.5, .1])
        optional = [np.array([-.8, .1, .3, .1]), np.array([.1, -.2, .7, -.3])]
        bound = export.all_offset_bound(prefix, fixed, optional)
        suffix_peak = 0.
        for mask in range(4):
            suffix = fixed + sum((x for j, x in enumerate(optional) if mask & (1 << j)), np.zeros(4))
            suffix_peak = max(suffix_peak, float(np.max(np.abs(suffix))))
            for delay in range(-12, 13):
                mixed = np.zeros(32)
                mixed[12:16] += prefix
                mixed[12 + delay:16 + delay] += suffix
                self.assertLessEqual(float(np.max(np.abs(mixed))), bound['peakUpperBound'])
        self.assertEqual(bound['peakUpperBound'], .6 + suffix_peak)

    def test_any_offset_bound_handles_absent_prefix_and_all_cancelled_suffix(self):
        self.assertEqual(export.all_offset_bound(np.zeros(4), np.ones(4) * .3, [])['peakUpperBound'], .3)
        # Gating may remove cancellation, so checking only the full-success sum is unsafe.
        b = export.all_offset_bound(np.zeros(4), np.ones(4) * .7, [-np.ones(4) * .7])
        self.assertEqual(b['peakUpperBound'], .7)

    def test_arbitrary_offset_safety_can_reduce_gain_beyond_grid_requirement(self):
        gain = export.runtime_safe_gain(.1, 2.)
        self.assertLessEqual(gain * 2., .95)
        self.assertLess(gain, export.common_safe_gain(.1))

    def test_arbitrary_offset_safety_preserves_grid_gain_when_already_safe(self):
        self.assertEqual(export.runtime_safe_gain(.9, 1.1), export.common_safe_gain(.9))


if __name__ == '__main__':
    unittest.main(verbosity=2)
