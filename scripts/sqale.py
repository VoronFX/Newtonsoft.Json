import json
import re
import collections
import xml.etree.ElementTree as E

SCOPE = [
    "JsonSerializerInternalReader.cs",
    "JsonSerializerInternalWriter.cs",
    "DefaultContractResolver.cs",
    "JsonSerializerInternalBase.cs",
    "JsonTypeReflector.cs",
    "JsonContract.cs",
    "JsonProperty.cs",
]

r = json.load(open('results/scope_results.json'))
m = json.load(open('results/metrics.json'))

# one entry per (rule, file, line); CC/coupling rules are handled separately below
u = {}
for i in r:
    if i['rule'] in ('CA1502', 'CA1505', 'CA1506', 'CA1501'):
        continue
    u[(i['rule'], i['file'], i['line'])] = i

# items: (category, subtype, file, cost)
items = []


def add(cat, sub, f, cost, ref=''):
    items.append(dict(cat=cat, sub=sub, file=f, cost=cost, ref=ref))


COST = {
    'fmt': 1, 'style': 5,
    'cc11_20': 60, 'cc21_40': 120, 'cc41': 240,
    'dup': 30, 'null': 30, 'dispose': 45,
    'argval': 10, 'argexc': 10, 'security': 30, 'obsolete': 60,
    'static': 5, 'api': 30, 'perf': 5, 'throwhelper': 5,
    'coupling_cls': 240, 'coupling_m': 60,
}

for (rule, f, l), i in u.items():
    if rule == 'IDE0055':
        add('Style', 'Formatting (IDE0055)', f, COST['fmt'], rule)
    elif rule.startswith('IDE'):
        add('Style', 'C# simplification/idioms (IDE00xx)', f, COST['style'], rule)
    elif rule.startswith('CS86') or rule.startswith('CS87'):
        add('Potential Defects', 'Possible null dereference (CS86xx)', f, COST['null'], rule)
    elif rule == 'CA2000':
        add('Potential Defects', 'IDisposable not disposed (CA2000)', f, COST['dispose'], rule)
    elif rule == 'CA1062':
        add('Potential Defects', 'Missing null argument check (CA1062)', f, COST['argval'], rule)
    elif rule == 'CA2208':
        add('Potential Defects', 'Incorrect argument exception (CA2208)', f, COST['argexc'], rule)
    elif rule == 'CA2326':
        add('Potential Defects', 'Unsafe TypeNameHandling (CA2326)', f, COST['security'], rule)
    elif rule in ('CS0618', 'SYSLIB0050'):
        add('Potential Defects', 'Use of obsolete API (CS0618/SYSLIB0050)', f, COST['obsolete'], rule)
    elif rule == 'CA1822':
        add('Design/Performance', 'Member can be made static (CA1822)', f, COST['static'], rule)
    elif rule == 'CA1002':
        add('Design/Performance', 'Public List<T> in API (CA1002)', f, COST['api'], rule)
    elif rule == 'CA1859':
        add('Design/Performance', 'Interface instead of concrete type (CA1859)', f, COST['perf'], rule)
    elif rule == 'CA1510':
        add('Design/Performance', 'Throw-helper ArgumentNullException (CA1510)', f, COST['throwhelper'], rule)
    else:
        print('UNMAPPED', rule)

for x in m['cc']:
    if x['cc'] > 40:
        add('Complexity', 'Method with M > 40', x['file'], COST['cc41'], x['name'])
    elif x['cc'] > 20:
        add('Complexity', 'Method with M = 21-40', x['file'], COST['cc21_40'], x['name'])
    elif x['cc'] > 10:
        add('Complexity', 'Method with M = 11-20', x['file'], COST['cc11_20'], x['name'])

for x in m['coupling']:
    types = {
        'JsonSerializerInternalReader', 'JsonSerializerInternalWriter', 'DefaultContractResolver',
        'JsonTypeReflector', 'JsonContract', 'JsonProperty', 'JsonSerializerInternalBase',
        'CreatorPropertyContext', 'CreatorCache', 'AssociatedMetadataTypesCache',
        'EnumerableDictionaryWrapper', 'ReferenceEqualsEqualityComparer',
    }
    if x['name'] in types:
        if x['coupling'] > 95:
            add('Complexity', 'Excessive class coupling (CA1506, >95 types)', x['file'], COST['coupling_cls'], x['name'])
    elif x['coupling'] > 40:
        add('Complexity', 'Excessive method coupling (CA1506, >40 types)', x['file'], COST['coupling_m'], x['name'])

ns = {'c': 'https://pmd-code.org/schema/cpd-report'}
dups = []
for d in E.parse('results/cpd_50.xml').getroot().findall('c:duplication', ns):
    fs = [(f.get('path').split('/')[-1], int(f.get('line')), int(f.get('endline'))) for f in d.findall('c:file', ns)]
    dups.append(dict(lines=int(d.get('lines')), tokens=int(d.get('tokens')), occ=fs))
    add('Duplication', 'Duplicated fragment (CPD >= 50 tokens)', fs[0][0], COST['dup'], f"{fs[0][1]}/{fs[1][1]}")

tot = sum(i['cost'] for i in items)

bysub = collections.OrderedDict()
for i in items:
    k = (i['cat'], i['sub'])
    b = bysub.setdefault(k, dict(cat=i['cat'], sub=i['sub'], n=0, cost=i['cost'], total=0))
    b['n'] += 1
    b['total'] += i['cost']

bycat = collections.Counter()
bycatn = collections.Counter()
for i in items:
    bycat[i['cat']] += i['cost']
    bycatn[i['cat']] += 1

matrix = collections.defaultdict(lambda: collections.Counter())
for i in items:
    matrix[i['file']][i['cat']] += i['cost']

# lines of code per scoped file (physical / non-comment / comment)
loc = {}
for f in SCOPE:
    s = open('Src/Newtonsoft.Json/Serialization/' + f, encoding='utf-8-sig').read()
    phys = s.count('\n') + 1
    s2 = re.sub(r'/\*.*?\*/', '', s, flags=re.S)
    code = [l.strip() for l in s2.split('\n') if l.strip() and not l.strip().startswith('//')]
    comm = [l for l in s.split('\n') if l.strip().startswith('//')]
    loc[f] = dict(phys=phys, ncloc=len(code), comment=len(comm))

ncloc = sum(v['ncloc'] for v in loc.values())
dev_min = ncloc * 30

out = dict(
    items=items,
    bysub=list(bysub.values()),
    bycat=dict(bycat),
    bycatn=dict(bycatn),
    total_min=tot,
    matrix={k: dict(v) for k, v in matrix.items()},
    loc=loc,
    ncloc=ncloc,
    dev_min=dev_min,
    tdr=tot / dev_min * 100,
    dups=dups,
    cost_table=COST,
)
json.dump(out, open('results/sqale.json', 'w'), indent=1, ensure_ascii=False)

for b in bysub.values():
    print(b)
print(dict(bycat), dict(bycatn))
print('total', tot, 'min =', tot / 60, 'h; ncloc', ncloc, 'dev h', dev_min / 60, 'TDR', round(tot / dev_min * 100, 2))
for f in SCOPE:
    print(f, loc[f], dict(matrix[f]), sum(matrix[f].values()))
