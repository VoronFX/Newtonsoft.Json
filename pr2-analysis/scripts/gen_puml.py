import json,collections
G=json.load(open('pr2-analysis/data/graph.json')); T={t['name']:t for t in G['types']}
cross=json.load(open('pr2-analysis/data/cross.json'))
def top(t):
    while T[t]['nested']: t=T[t]['container']
    return t
def comp(t):
    f=T[t]['folder'].replace('\\','/'); return {'':'Core','Linq/JsonPath':'Linq.JsonPath'}.get(f,f)
tops=[t for t in T if not T[t]['nested']]
info={}
for c in ['Utilities','Core','Linq','Linq.JsonPath','Bson','Serialization','Converters','Schema']:
    ts=[t for t in tops if comp(t)==c]
    info[c]=dict(types=len(ts),lines=sum(T[t]['lines'] for t in ts),obs=sum(T[t]['obsolete'] for t in ts))
deg=collections.Counter()
for x in cross: deg[x['src']]+=x['n']; deg[x['dst']]+=x['n']
desc={'Core':'JsonReader/Writer, JsonTextReader/Writer,\\nJsonSerializer, JsonConvert, атрибути, enum',
'Serialization':'контракти, DefaultContractResolver,\\nInternalReader/Writer','Linq':'JToken, JObject, JArray, JValue',
'Linq.JsonPath':'JPath, фільтри запитів','Converters':'JsonConverter-и (Xml, DataSet,\\nRegex, Enum, Date, Union…)',
'Utilities':'ReflectionUtils, ConvertUtils,\\nStringUtils, MiscellaneousUtils…','Bson':'BsonReader/Writer [Obsolete]','Schema':'JsonSchema* [Obsolete]'}
alias={c:c.replace('.','_') for c in info}
def color(c):
    d=deg[c]
    if d>=400: return '#E8590C'   # хаб
    if d>=200: return '#FFA94D'
    if d>=80:  return '#FFD8A8'
    return '#FFF4E6'
L=['@startuml components_packages','!pragma layout dot' if False else '','skinparam defaultFontName "DejaVu Sans"','skinparam componentStyle rectangle','skinparam shadowing false',
'skinparam ArrowColor #868E96','skinparam ArrowFontSize 11','skinparam ComponentBorderColor #495057','skinparam nodesep 40','skinparam ranksep 70',
'title Діаграма компонентів Src/Newtonsoft.Json (рівень пакетів)\\nкоміт 52fa3aef; ребро = кількість пар «тип → тип» між пакетами (Roslyn)','left to right direction' if False else 'top to bottom direction']
L.append('package "Newtonsoft.Json.dll" {')
for c,i in info.items():
    obs=';line.dashed;line:495057' if c in('Bson','Schema') else ''
    border=';line:C92A2A;line.bold' if deg[c]>=400 else ''
    L.append(f'  component "**{c}**\\n{desc[c]}\\n<size:10>{i["types"]} типів · {i["lines"]} рядків · Ca+Ce={deg[c]}</size>" as {alias[c]} {color(c)}{border if not obs else obs}')
L.append('}')
for x in sorted(cross,key=lambda x:-x['n']):
    a,b,n,k=alias[x['src']],alias[x['dst']],x['n'],x['kind']
    if k=='down':
        w='thickness=3' if n>=100 else ('thickness=2' if n>=40 else 'thickness=1')
        L.append(f'{a} -[#868E96,{w}]-> {b} : {n}')
    else:
        L.append(f'{a} -[#C92A2A,dashed,thickness=2]-> {b} : <color:#C92A2A>**{n}**</color>')
L+=['legend right','|= Позначення |= Значення |',
'|<#E8590C>      | хаб: Ca+Ce ≥ 400 пар залежностей (жирна червона рамка) |',
'|<#FFA94D>      | 200–399 пар |','|<#FFD8A8>      | 80–199 пар |','|<#FFF4E6>      | < 80 пар |',
'| <color:#868E96>—→</color> | залежність «згори вниз» за очікуваними шарами (товщина ~ кількість) |',
'| <color:#C92A2A>- - →</color> | залежність проти шарів / між рівноправними пакетами (утворює цикл) |',
'| пунктирна рамка | пакет повністю позначений [Obsolete] |',
'| шари | Utilities(0) ← Core(1) ← Linq, Bson, Serialization(2) ← Linq.JsonPath, Converters, Schema(3) |',
'endlegend','@enduml']
open('pr2-analysis/diagrams/components_packages.puml','w').write('\n'.join(l for l in L if l!=''))
print(deg)
