"""Bounded source analysis of current reagent outcomes, not a Unity test.

Run from any directory with Python 3. The saved receipt records the source hashes.
The translated operation is outcome classification, not brewing effects or damage.
MAX merging means property-presence unions suffice for the no-effect branch.
"""

import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
FILES = [
    "Assets/Resources/Content/Blueprints/Objects.json",
    "Assets/Resources/Content/Data/Alchemy/BrewRules.json",
    "Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs",
    "Assets/Scripts/Gameplay/Alchemy/BrewingService.cs",
    "Assets/Scripts/Gameplay/Alchemy/ReagentPart.cs",
    "Assets/Scripts/Gameplay/Alchemy/BrewProperty.cs",
    "Assets/Scripts/Gameplay/Alchemy/BrewRuleRegistry.cs",
]


def parse_properties(raw):
    """BrewPropertyAmount.ParseList plus BrewResolver's positive MAX merge."""
    result = {}
    for entry in re.split(r"[,;|]", raw or ""):
        name, colon, amount = entry.strip().partition(":")
        name = name.strip().lower()
        if not name:
            continue
        potency = 1
        if colon:
            amount = amount.strip()
            if not re.fullmatch(r"[+-]?[0-9]+", amount):
                continue
            potency = int(amount)
            if not -(2**31) <= potency < 2**31:
                continue
        if potency > 0:
            result[name] = max(result.get(name, 0), potency)
    return result


def split_rule(raw):
    return {p.strip().lower() for p in re.split(r"[ ,;|]", raw or "") if p.strip()}


def main():
    objects = json.loads((ROOT / FILES[0]).read_text())["Objects"]
    raw = {obj["Name"]: obj for obj in objects}
    baked = {}

    def resolve(name):
        if name in baked:
            return baked[name]
        obj = raw[name]
        parent = resolve(obj["Inherits"]) if obj.get("Inherits") in raw else {}
        parts = {key: dict(value) for key, value in parent.items()}
        for part in obj.get("Parts", []):
            parts.setdefault(part["Name"], {}).update(
                {param["Key"]: param["Value"] for param in part.get("Params", [])}
            )
        baked[name] = parts
        return parts

    inputs = []
    for name in raw:
        parts = resolve(name)
        if "Reagent" in parts:
            inputs.append({"id": name, "properties": parse_properties(parts["Reagent"].get("PropertiesRaw", ""))})

    # Registry behavior: skip blank IDs; duplicate IDs replace in place.
    rules = {}
    for rule in json.loads((ROOT / FILES[1]).read_text())["Rules"]:
        if rule and rule.get("ID", "").strip():
            rules[rule["ID"].lower()] = rule

    # Track nonempty selections separately from an empty selection, including a
    # possible empty property profile. This also detects future empty reagents.
    states = {}
    for item in inputs:
        additions = [(frozenset(item["properties"]), [item["id"]])]
        additions.extend((props | frozenset(item["properties"]), witness + [item["id"]])
                         for props, witness in list(states.items()))
        for props, witness in additions:
            states.setdefault(props, witness)

    outcomes = {"Brew": 0, "InertSludge": 0, "Mishap": 0}
    no_effect = []
    for props, witness in states.items():
        matched = any(
            rule.get("Effect", "").strip()
            and split_rule(rule.get("RequireAll"))
            and split_rule(rule.get("RequireAll")) <= props
            and not split_rule(rule.get("ForbidAny")) & props
            for rule in rules.values()
        )
        outcome = "Brew" if matched else "Mishap" if "volatile" in props else "InertSludge"
        outcomes[outcome] += 1
        if not matched:
            no_effect.append({"properties": sorted(props), "witness": witness, "outcome": outcome})

    report = {
        "method": "Exhaustive property-presence closure of nonempty selections of current authored reagents. Parse positive Int32 potencies with MAX merge; match required/forbidden presence exactly. More copies cannot create new profiles. Does not execute Unity.",
        "input_sha256": {path: hashlib.sha256((ROOT / path).read_bytes()).hexdigest() for path in FILES},
        "reagents": inputs,
        "distinct_profiles_from_nonempty_selections": len(states),
        "outcomes": outcomes,
        "no_effect_witnesses": no_effect,
        "bound": "Current Objects.json and BrewRules.json only. Saved, modded or runtime-injected profiles/rules could change the result. Acquisition and the absence of such normal-play injection were reviewed separately. This does not test command costs, mishap damage, UI, balance or native runtime behavior.",
    }
    Path(__file__).with_name("reagent_reachability.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({key: report[key] for key in ("distinct_profiles_from_nonempty_selections", "outcomes", "no_effect_witnesses")}, indent=2))


if __name__ == "__main__":
    main()
