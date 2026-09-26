import json
import collections
import re

SCOPE = [
    "JsonSerializerInternalReader.cs",
    "JsonSerializerInternalWriter.cs",
    "DefaultContractResolver.cs",
    "JsonSerializerInternalBase.cs",
    "JsonTypeReflector.cs",
    "JsonContract.cs",
    "JsonProperty.cs",
]

d = json.load(open('results/analysis.sarif'))
run = d['runs'][0]

rules = {}
for r in run['tool']['driver']['rules']:
    rules[r['id']] = r

res = []
seen = set()
for x in run['results']:
    if not x.get('locations'):
        continue
    loc = x['locations'][0]['physicalLocation']
    uri = loc['artifactLocation']['uri']
    f = uri.split('/')[-1]
    if '/Serialization/' not in uri or f not in SCOPE:
        continue

    line = loc['region']['startLine']
    col = loc['region'].get('startColumn')
    key = (x['ruleId'], f, line, col, x['message']['text'])
    if key in seen:
        continue
    seen.add(key)
    res.append(dict(rule=x['ruleId'], file=f, line=line, msg=x['message']['text']))

json.dump(res, open('results/scope_results.json', 'w'), indent=1, ensure_ascii=False)

c = collections.Counter(r['rule'] for r in res)
for k, v in c.most_common():
    rr = rules.get(k, {})
    print(k, v, '|', rr.get('shortDescription', {}).get('text', '')[:90], '|', rr.get('properties', {}).get('category', ''))
