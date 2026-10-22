import json,collections
G=json.load(open('pr2-analysis/data/graph.json')); T={t['name']:t for t in G['types']}
agg=json.load(open('pr2-analysis/data/agg.json'))
sub={tuple(k.split('|')):v for k,v in json.load(open('pr2-analysis/data/sub.json')).items()}
LAYER={'Utilities':0,'Core':1,'Linq':2,'Bson':2,'Serialization':2,'Linq.JsonPath':3,'Converters':3,'Schema':3}
full={n.split('.')[-1]:n for n in T if not T[n]['nested']}
def comp(s):
    f=T[full[s]]['folder'].replace('\\','/'); return {'':'Core','Linq/JsonPath':'Linq.JsonPath'}.get(f,f)
NODES=['JsonConvert','JsonSerializer','JsonReader','JsonTextReader','JsonWriter','JsonValidatingReader',
'JsonSerializerInternalReader','JsonSerializerInternalWriter','JsonSerializerInternalBase','JsonSerializerProxy','DefaultContractResolver','JsonContract','JsonProperty','JsonTypeReflector',
'JToken','JContainer','JPath','JsonConverter','XmlNodeConverter','RegexConverter','BsonObjectIdConverter','JsonSchemaGenerator','BsonWriter','ReflectionUtils','ReflectionObject']
deg={k.split('.')[-1]:v for k,v in agg['deg'].items()}
def fill(s):
    i,o=deg[s]; t=i+o
    if t>=60: return '#E8590C'
    if t>=35: return '#FFA94D'
    if t>=15: return '#FFD8A8'
    return '#FFF4E6'
L=['@startuml components_classes','skinparam defaultFontName "DejaVu Sans"','skinparam componentStyle rectangle','skinparam shadowing false','skinparam ArrowColor #ADB5BD','skinparam ArrowFontSize 10','skinparam nodesep 18','skinparam ranksep 90','left to right direction',
'title Діаграма ключових компонентів-класів (фрагмент графа залежностей, коміт 52fa3aef)\\nу дужках: Ca / Ce — кількість типів бібліотеки, що залежать від класу / від яких залежить клас']
pk=collections.defaultdict(list)
for s in NODES: pk[comp(s)].append(s)
for p in ['Core','Serialization','Linq','Linq.JsonPath','Converters','Schema','Bson','Utilities']:
    if p not in pk: continue
    L.append(f'package "{p}" {{')
    for s in pk[p]:
        t=T[full[s]]; i,o=deg[s]
        st=fill(s)
        if t['obsolete']: st+=';line.dashed'
        if i+o>=60: st+=';line:C92A2A;line.bold'
        L.append(f'  component "{s}\\n<size:10>({i} / {o})</size>" as {s} {st}')
    L.append('}')
keep=0
for (a,b),k in sub.items():
    if a not in NODES or b not in NODES: continue
    w=sum(k.values()); ca,cb=comp(a),comp(b)
    viol = ca!=cb and LAYER[ca]<=LAYER[cb]
    inh='inherit' in k
    if viol:
        L.append(f'{a} -[#C92A2A,dashed,thickness=2]-> {b}'); keep+=1
    elif inh:
        L.append(f'{a} -[#1C7ED6]-|> {b}'); keep+=1
    elif w>=25:
        L.append(f'{a} -[#868E96,thickness={2 if w>=100 else 1}]-> {b}'); keep+=1
L+=['legend right','|= Позначення |= Значення |','|<#E8590C>    | Ca+Ce ≥ 60 (хаб, жирна червона рамка) |','|<#FFA94D>    | 35–59 |','|<#FFD8A8>    | 15–34 |','|<#FFF4E6>    | < 15 |',
'| <color:#1C7ED6>—▷</color> | успадкування |','| <color:#868E96>—→</color> | сильна залежність (≥ 25 звернень; товста ≥ 100) |',
'| <color:#C92A2A>- - →</color> | залежність проти шарів / між рівноправними пакетами |','| пунктирна рамка | [Obsolete] |',
'| примітка | ребра до MiscellaneousUtils/StringUtils/ValidationUtils не показані |','endlegend','@enduml']
open('components_classes.puml','w').write('\n'.join(L)); print('edges',keep)
