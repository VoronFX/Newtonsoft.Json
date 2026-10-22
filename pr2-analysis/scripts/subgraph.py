import json,collections
G=json.load(open('pr2-analysis/data/graph.json')); T={t['name']:t for t in G['types']}
def top(t):
    while T[t]['nested']: t=T[t]['container']
    return t
KEY=['JsonConvert','JsonSerializer','JsonSerializerSettings','JsonReader','JsonTextReader','JsonWriter','JsonTextWriter','JsonValidatingReader',
'JsonSerializerInternalReader','JsonSerializerInternalWriter','JsonSerializerInternalBase','JsonSerializerProxy','DefaultContractResolver','JsonContract','JsonProperty','JsonTypeReflector',
'JToken','JContainer','JObject','JPath','JsonConverter','XmlNodeConverter','RegexConverter','BsonObjectIdConverter','JsonSchema','JsonSchemaGenerator','BsonWriter','BsonReader','ReflectionUtils','ConvertUtils','MiscellaneousUtils','ReflectionObject','EnumUtils']
full={}
for n in T:
    s=n.split('.')[-1]
    if s in KEY and not T[n]['nested']: full[s]=n
missing=[k for k in KEY if k not in full]; print('missing',missing)
E=collections.defaultdict(collections.Counter)
for e in G['edges']:
    a,b=top(e['from']).split('.')[-1],top(e['to']).split('.')[-1]
    if a in full and b in full and a!=b and full[a]==top(e['from']) and full[b]==top(e['to']): E[(a,b)][e['kind']]+=e['count']
print(len(E))
for (a,b),k in sorted(E.items()): print(a,'->',b,dict(k))
json.dump({f"{a}|{b}":dict(k) for (a,b),k in E.items()},open('pr2-analysis/data/sub.json','w'),indent=0)
