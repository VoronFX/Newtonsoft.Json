import json
import re
import collections

results = json.load(open('results/scope_results.json'))

cc = []
mi = []
cp = []
for i in results:
    m = re.match(r"'(.+?)' has a cyclomatic complexity of '(\d+)'", i['msg'])
    if m and i['rule'] == 'CA1502':
        cc.append(dict(name=m.group(1), cc=int(m.group(2)), file=i['file'], line=i['line']))

    m = re.match(r"'(.+?)' has a maintainability index of '(\d+)'", i['msg'])
    if m:
        mi.append(dict(name=m.group(1), mi=int(m.group(2)), file=i['file'], line=i['line']))

    m = re.match(r"'(.+?)' is coupled with '(\d+)' different types from '(\d+)'", i['msg'])
    if m:
        cp.append(dict(name=m.group(1), coupling=int(m.group(2)), ns=int(m.group(3)), file=i['file'], line=i['line']))


def dedup(items):
    seen = {}
    for x in items:
        seen[(x['name'], x['file'], x['line'])] = x
    return list(seen.values())


cc = dedup(cc)
mi = dedup(mi)
cp = dedup(cp)
cc.sort(key=lambda x: -x['cc'])

json.dump(dict(cc=cc, mi=mi, coupling=cp), open('results/metrics.json', 'w'), indent=1)

print(len(cc), 'methods CC>1; >10:', sum(x['cc'] > 10 for x in cc), '>20:', sum(x['cc'] > 20 for x in cc))
for x in cc[:30]:
    print(x)

print('--MI lowest')
for x in sorted(mi, key=lambda x: x['mi'])[:15]:
    print(x)

print('--coupling highest')
for x in sorted(cp, key=lambda x: -x['coupling'])[:15]:
    print(x)
