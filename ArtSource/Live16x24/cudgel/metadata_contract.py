"""Compare authored Unity metadata semantically without changing either file.

YAML parsing uses PyYAML when installed, otherwise Ruby's bundled safe Psych
parser. Only parsing depends on these libraries; comparisons use plain values.
"""
import json
import shutil
import subprocess


def parse_metadata(text):
    try:
        import yaml
    except ModuleNotFoundError:
        ruby = shutil.which('ruby')
        if not ruby:
            raise RuntimeError('Metadata verification requires PyYAML or Ruby with Psych; neither is available.')
        parsed = subprocess.run(
            [ruby, '-rpsych', '-rjson', '-e', 'STDOUT.write(JSON.generate(Psych.safe_load(STDIN.read)))'],
            input=text, text=True, capture_output=True, check=True)
        result = json.loads(parsed.stdout)
    else:
        result = yaml.safe_load(text)
    assert isinstance(result, dict), 'Unity metadata must be a mapping'
    return result


def assert_authored_subset(authored, imported, path='metadata'):
    """Permit added mapping defaults, never changed authored values/geometry.

Every authored mapping key retains its original hierarchy. Explicit empty
maps/lists remain empty. Lists retain length and order, preventing added sprite
geometry, remaps, or platform overrides from masquerading as default fields.
"""
    if isinstance(authored, dict):
        assert isinstance(imported, dict), (path, 'expected mapping')
        if not authored:
            assert not imported, (path, 'authored empty mapping changed')
        for key, value in authored.items():
            assert key in imported, (path+'.'+key, 'missing authored key')
            assert_authored_subset(value, imported[key], path+'.'+key)
    elif isinstance(authored, list):
        assert isinstance(imported, list), (path, 'expected list')
        assert len(authored) == len(imported), (path, 'authored list structure changed')
        for index, value in enumerate(authored):
            assert_authored_subset(value, imported[index], path+'['+str(index)+']')
    else:
        numeric = lambda value: isinstance(value, (int, float)) and not isinstance(value, bool)
        assert type(authored) is type(imported) or numeric(authored) and numeric(imported), (path, 'authored scalar type changed')
        assert authored == imported, (path, 'authored scalar value changed', authored, imported)


def authored_contract_counts(value):
    counts = {'mappingKeys': 0, 'scalarValues': 0, 'lists': 0, 'explicitEmptyContainers': 0}

    def visit(node):
        if isinstance(node, dict):
            counts['mappingKeys'] += len(node)
            if not node: counts['explicitEmptyContainers'] += 1
            for child in node.values(): visit(child)
        elif isinstance(node, list):
            counts['lists'] += 1
            if not node: counts['explicitEmptyContainers'] += 1
            for child in node: visit(child)
        else:
            counts['scalarValues'] += 1
    visit(value)
    return counts
