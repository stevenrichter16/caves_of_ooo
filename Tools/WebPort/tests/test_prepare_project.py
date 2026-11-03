"""Exercise the real exporter against disposable Git repositories.

Run: python3 -m unittest discover -s Tools/WebPort/tests -p 'test_*.py' -v
These checks do not require or qualify a Unity build.
"""
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
import xml.etree.ElementTree as ET


_SPEC = importlib.util.spec_from_file_location(
    "coo_prepare_project", Path(__file__).resolve().parents[1] / "prepare_project.py")
prepare_project = importlib.util.module_from_spec(_SPEC)
_SPEC.loader.exec_module(prepare_project)

MCP_TOOLS = (
    "Assets/Editor/CodexMcpKickstart.cs",
    "Assets/Editor/Diagnostics/DiagQueryTool.cs",
    "Assets/Editor/Diagnostics/DiagCountTool.cs",
    "Assets/Editor/Diagnostics/DiagAssertTool.cs",
    "Assets/Editor/Diagnostics/DiagInspectRecordTool.cs",
)


class PrepareProjectTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="coo-web-prepare-")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.repo = self.root / "desktop"
        self.repo.mkdir()
        self.output = self.root / "phone-build-project"
        self.git("init", "-q")
        self.git("config", "user.name", "Web export test")
        self.git("config", "user.email", "web-export-test@example.invalid")
        self.git("config", "commit.gpgsign", "false")
        self.git("config", "core.autocrlf", "false")
        self.git("config", "core.hooksPath", str(self.root / "unused-hooks"))
        self.write("Assets/Scenes/Main/SampleScene.unity", "original scene\n")
        self.write("Assets/Editor/WebPhoneBuild.cs", "// committed Web build entry\n")
        self.write("Assets/Editor/KeepThisTool.cs", "// unrelated editor tool\n")
        self.write("Assets/WebGLTemplates/CavesPhone/index.html", "<title>Caves</title>\n")
        self.write("Assets/WebGLTemplates/CavesPhone/service-worker.js", "const build='__COO_BUILD_ID__';\n")
        self.write("Assets/WebGLTemplates/CavesPhone/phone.js", "// phone shell\n")
        self.write("Assets/WebGLTemplates/CavesPhone/phone.css", "body { margin: 0; }\n")
        self.write("Assets/WebGLTemplates/CavesPhone/web-save-store.js", "// browser save storage\n")
        self.write("Assets/WebGLTemplates/CavesPhone/manifest.webmanifest", "{}\n")
        self.write("Assets/WebGLTemplates/CavesPhone/icon.svg", '<svg xmlns="http://www.w3.org/2000/svg"/>\n')
        self.write("Assets/Scripts/Player.cs", "// first committed player\n")
        self.write("Assets/Resources/example.bytes", b"\x00\x01\xff\n")
        self.write("Docs/not-a-build-input.md", "Keep outside exported project.\n")
        self.write("ProjectSettings/ProjectVersion.txt", "m_EditorVersion: 6000.3.4f1\n")
        self.write_json("Packages/manifest.json", {"dependencies": {
            "com.coplaydev.unity-mcp": "file:/Users/steven/unity-mcp/MCPForUnity",
            "com.unity.render-pipelines.universal": "17.3.0",
        }})
        self.write_json("Packages/packages-lock.json", {"dependencies": {
            "com.coplaydev.unity-mcp": {
                "version": "file:/Users/steven/unity-mcp/MCPForUnity",
                "depth": 0, "source": "local", "dependencies": {},
            },
            "com.unity.nuget.newtonsoft-json": {
                "version": "3.2.2", "depth": 1, "source": "registry",
                "dependencies": {}, "url": "https://packages.unity.com",
            },
            "com.unity.render-pipelines.universal": {
                "version": "17.3.0", "depth": 0, "source": "registry",
                "dependencies": {}, "url": "https://packages.unity.com",
            },
        }})
        for index, path in enumerate(MCP_TOOLS):
            self.write(path, "// machine-local MCP tool\n")
            self.write(path + ".meta", "guid: fixture-" + str(index) + "\n")
        self.commit = self.commit_all("complete web fixture")

    def git(self, *args):
        return subprocess.check_output(
            ["git", "-C", str(self.repo), *args], stderr=subprocess.PIPE).decode().strip()

    def write(self, relative, contents):
        path = self.repo / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(contents if isinstance(contents, bytes) else contents.encode())

    def write_json(self, relative, value):
        self.write(relative, json.dumps(value, indent=2) + "\n")

    def commit_all(self, message):
        self.git("add", "--all")
        self.git("commit", "-qm", message)
        return self.git("rev-parse", "HEAD")

    def source_files(self):
        return {str(path.relative_to(self.repo)): path.read_bytes()
                for path in self.repo.rglob("*")
                if path.is_file() and ".git" not in path.relative_to(self.repo).parts}

    def export(self, ref=None, output=None, dry_run=False):
        return prepare_project.prepare(
            self.repo, ref or self.commit, output or self.output, dry_run)

    def test_exports_requested_commit_even_when_head_and_worktree_have_changed(self):
        self.write("Assets/Scripts/Player.cs", "// second committed player\n")
        self.commit_all("later desktop changes")
        self.write("Assets/Scripts/Player.cs", "// uncommitted player\n")
        self.write("Assets/Scripts/Untracked.cs", "// untracked\n")
        before = self.source_files()
        status_before = self.git("status", "--porcelain")

        report = self.export()

        self.assertEqual(self.commit, report["source"])
        self.assertEqual("// first committed player\n",
                         (self.output / "Assets/Scripts/Player.cs").read_text())
        self.assertFalse((self.output / "Assets/Scripts/Untracked.cs").exists())
        self.assertEqual(before, self.source_files())
        self.assertEqual(status_before, self.git("status", "--porcelain"))

    def test_excludes_only_the_five_mcp_tools_and_their_metadata(self):
        before = self.source_files()
        self.export()
        for path in MCP_TOOLS:
            with self.subTest(path=path):
                self.assertFalse((self.output / path).exists())
                self.assertFalse((self.output / (path + ".meta")).exists())
                self.assertTrue((self.repo / path).is_file())
                self.assertTrue((self.repo / (path + ".meta")).is_file())
        self.assertEqual(b"// unrelated editor tool\n",
                         (self.output / "Assets/Editor/KeepThisTool.cs").read_bytes())
        self.assertEqual(b"\x00\x01\xff\n", (self.output / "Assets/Resources/example.bytes").read_bytes())
        self.assertFalse((self.output / "Docs").exists())
        self.assertEqual(before, self.source_files())

    def test_removes_local_mcp_and_pins_newtonsoft_without_changing_other_packages(self):
        original = json.loads((self.repo / "Packages/packages-lock.json").read_text())
        self.export()
        manifest = json.loads((self.output / "Packages/manifest.json").read_text())["dependencies"]
        lock = json.loads((self.output / "Packages/packages-lock.json").read_text())["dependencies"]
        self.assertNotIn("com.coplaydev.unity-mcp", manifest)
        self.assertNotIn("com.coplaydev.unity-mcp", lock)
        self.assertEqual("3.2.2", manifest["com.unity.nuget.newtonsoft-json"])
        self.assertEqual("3.2.2", lock["com.unity.nuget.newtonsoft-json"]["version"])
        self.assertEqual(0, lock["com.unity.nuget.newtonsoft-json"]["depth"])
        self.assertEqual("17.3.0", manifest["com.unity.render-pipelines.universal"])
        self.assertEqual(original["dependencies"]["com.unity.render-pipelines.universal"],
                         lock["com.unity.render-pipelines.universal"])

    def test_writes_provenance_and_reflection_preservation(self):
        report = self.export()
        marker = json.loads((self.output / ".coo-web-project.json").read_text())
        self.assertEqual(report, marker)
        self.assertEqual(self.commit, marker["source"])
        self.assertIn("6000.3.4f1", marker["editor"])
        self.assertEqual(set(MCP_TOOLS), set(marker["excludedEditorTools"]))
        self.assertEqual(hashlib.sha256((self.output / "Packages/manifest.json").read_bytes()).hexdigest(),
                         marker["manifestSha256"])
        linker = ET.parse(self.output / "Assets/WebPortPreservation/link.xml").getroot()
        self.assertEqual("linker", linker.tag)
        self.assertEqual({"fullname": "CavesOfOoo", "preserve": "all"}, linker.find("assembly").attrib)

    def test_refuses_existing_empty_or_populated_output_without_changes(self):
        for populated in (False, True):
            with self.subTest(populated=populated):
                output = self.root / ("existing-full" if populated else "existing-empty")
                output.mkdir()
                if populated:
                    (output / "keep.bin").write_bytes(b"do not replace")
                before = {p.name: p.read_bytes() for p in output.iterdir()}
                with self.assertRaisesRegex(ValueError, "new directory"):
                    self.export(output=output)
                self.assertEqual(before, {p.name: p.read_bytes() for p in output.iterdir()})

    def test_refuses_commit_missing_web_entry_or_template_before_creating_output(self):
        for index, path in enumerate(("Assets/Editor/WebPhoneBuild.cs",
                                      "Assets/WebGLTemplates/CavesPhone/index.html")):
            with self.subTest(path=path):
                original = (self.repo / path).read_bytes()
                self.git("rm", path)
                incomplete = self.commit_all("incomplete web fixture " + str(index))
                output = self.root / ("incomplete-" + str(index))
                try:
                    with self.assertRaisesRegex(ValueError, "complete Web port"):
                        self.export(ref=incomplete, output=output)
                    self.assertFalse(output.exists())
                finally:
                    self.write(path, original)
                    self.commit_all("restore complete fixture")

    def test_refuses_commit_missing_a_runtime_shell_dependency(self):
        for index, name in enumerate(("phone.js", "phone.css", "web-save-store.js",
                                      "service-worker.js", "manifest.webmanifest", "icon.svg")):
            with self.subTest(name=name):
                path = "Assets/WebGLTemplates/CavesPhone/" + name
                original = (self.repo / path).read_bytes()
                self.git("rm", path)
                incomplete = self.commit_all("missing shell dependency " + name)
                output = self.root / ("missing-shell-" + str(index))
                try:
                    with self.assertRaisesRegex(ValueError, "complete Web port"):
                        self.export(ref=incomplete, output=output)
                    self.assertFalse(output.exists())
                finally:
                    self.write(path, original)
                    self.commit_all("restore complete shell")

    def test_dry_run_validates_commit_without_creating_or_mutating_files(self):
        before = self.source_files()
        report = self.export(dry_run=True)
        self.assertEqual(self.commit, report["source"])
        self.assertFalse(self.output.exists())
        self.assertEqual(before, self.source_files())

    def test_exports_without_a_lockfile(self):
        self.git("rm", "Packages/packages-lock.json")
        commit = self.commit_all("no lockfile")
        self.export(ref=commit)
        self.assertFalse((self.output / "Packages/packages-lock.json").exists())
        manifest = json.loads((self.output / "Packages/manifest.json").read_text())
        self.assertEqual("3.2.2", manifest["dependencies"]["com.unity.nuget.newtonsoft-json"])

    @unittest.skipUnless(os.name == "posix", "POSIX Git executable mode")
    def test_preserves_executable_mode(self):
        path = "Assets/Plugins/helper.sh"
        self.write(path, "#!/bin/sh\nexit 0\n")
        (self.repo / path).chmod(0o755)
        commit = self.commit_all("executable build asset")
        self.export(ref=commit)
        self.assertEqual(0o755, (self.output / path).stat().st_mode & 0o777)

    @unittest.skipUnless(os.name == "posix", "POSIX symlink fixture")
    def test_refuses_symlinks_inside_exported_roots(self):
        (self.repo / "Assets/escape").symlink_to(self.root)
        commit = self.commit_all("unsupported symlink")
        with self.assertRaisesRegex(ValueError, "Unsupported project entry"):
            self.export(ref=commit)
        self.assertFalse(self.output.exists())

    def test_refuses_output_inside_desktop_checkout(self):
        before = self.source_files()
        with self.assertRaisesRegex(ValueError, "[Oo]utside|[Ss]ource|[Cc]heckout"):
            self.export(output=self.repo / "Assets/AccidentalCopy")
        self.assertEqual(before, self.source_files())

    def test_existing_newtonsoft_lock_version_agrees_with_requested_pin(self):
        lock = json.loads((self.repo / "Packages/packages-lock.json").read_text())
        lock["dependencies"]["com.unity.nuget.newtonsoft-json"]["version"] = "3.0.2"
        self.write_json("Packages/packages-lock.json", lock)
        commit = self.commit_all("older resolved dependency")
        self.export(ref=commit)
        exported = json.loads((self.output / "Packages/packages-lock.json").read_text())
        self.assertEqual("3.2.2", exported["dependencies"]["com.unity.nuget.newtonsoft-json"]["version"])


if __name__ == "__main__":
    unittest.main()
