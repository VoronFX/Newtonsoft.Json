import json,collections
G=json.load(open('pr2-analysis/data/graph.json')); T={t['name']:t for t in G['types']}
def top(t):
    while T[t]['nested']: t=T[t]['container']
    return t
def comp(t):
    f=T[t]['folder'].replace('\\','/'); return {'':'Core','Linq/JsonPath':'Linq.JsonPath'}.get(f,f)
LAYER={'Utilities':0,'Core':1,'Linq':2,'Bson':2,'Serialization':2,'Linq.JsonPath':3,'Converters':3,'Schema':3}
pairs=collections.defaultdict(lambda: collections.defaultdict(set))
for e in G['edges']:
    a,b=top(e['from']),top(e['to'])
    if a==b: continue
    ca,cb=comp(a),comp(b)
    if ca!=cb: pairs[(ca,cb)][a.split('.')[-1]].add(b.split('.')[-1])
tot=0;up=0;peer=0
res=[]
for (ca,cb),m in sorted(pairs.items()):
    n=sum(len(v) for v in m.values()); tot+=n
    kind='down' if LAYER[ca]>LAYER[cb] else ('peer' if LAYER[ca]==LAYER[cb] else 'UP')
    if kind=='UP': up+=n
    if kind=='peer': peer+=n
    res.append(dict(src=ca,dst=cb,n=n,kind=kind,examples={k:sorted(v) for k,v in m.items()}))
    if kind!='down': print(f"{kind:4} {ca}->{cb} ({n}):", '; '.join(f"{k}->{','.join(sorted(v)[:4])}" for k,v in sorted(m.items(),key=lambda x:-len(x[1]))[:6]))
print('total cross edges',tot,'up',up,'peer',peer)
json.dump(res,open('pr2-analysis/data/cross.json','w'),indent=1)
