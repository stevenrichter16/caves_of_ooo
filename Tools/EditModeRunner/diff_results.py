"""diff_results.py before.xml after.xml — tests whose outcome changed."""
import sys, xml.etree.ElementTree as ET
def load(p): return {t.get('fullname'): t.get('result') for t in ET.parse(p).getroot().iter('test-case')}
b, a = load(sys.argv[1]), load(sys.argv[2])
broke = sorted(k for k in a if a[k] == 'Failed' and b.get(k) != 'Failed')
fixed = sorted(k for k in a if a[k] != 'Failed' and b.get(k) == 'Failed')
print(f'NEWLY FAILING: {len(broke)}'); [print('  ' + k) for k in broke]
print(f'NEWLY PASSING: {len(fixed)}'); [print('  ' + k) for k in fixed]
sys.exit(1 if broke else 0)
