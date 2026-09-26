import json
import collections

r = json.load(open('results/scope_results.json'))
m = json.load(open('results/metrics.json'))
s = json.load(open('results/sqale.json'))
cct = json.load(open('results/cc_table.json'))
types = json.load(open('results/types.json'))

u = {}
for i in r:
    if i['rule'] in ('CA1502', 'CA1505', 'CA1506', 'CA1501'):
        continue
    u[(i['rule'], i['file'], i['line'])] = i
rc = collections.Counter(k[0] for k in u)

cls = {t['type'].split('.')[-1]: t for t in types}
cpl = {x['name']: x for x in m['coupling']}

cls_rows = []
for n in ['JsonSerializerInternalReader', 'DefaultContractResolver', 'JsonSerializerInternalWriter',
          'JsonTypeReflector', 'JsonContract', 'JsonProperty', 'JsonSerializerInternalBase']:
    t = cls[n]
    ce = cpl[n]['coupling']
    ca = t['ca']
    cls_rows.append(dict(
        name=n,
        methods=t['ordinaryMethods'],
        nonpriv=len(t['nonPrivateMethods']),
        attrs=len(t['fields']) + len(t['autoProps']),
        inh=len(t['inheritedAttrs']),
        lcomhs=t['lcomHS'],
        lcom4=t['lcom4'],
        ce=ce,
        ns=cpl[n]['ns'],
        ca=ca,
        I=round(ce / (ce + ca), 2),
    ))

out = dict(
    rule_counts=dict(rc),
    sqale=s,
    cc=m['cc'],
    cc_table=cct,
    classes=cls_rows,
    mi={x['name']: x['mi'] for x in m['mi']},
)
json.dump(out, open('results/report_data.json', 'w'), ensure_ascii=False, indent=1)

print(rc.most_common())
print(cls_rows)
