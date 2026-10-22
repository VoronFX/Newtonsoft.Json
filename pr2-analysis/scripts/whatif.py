import json,collections,sys
sys.setrecursionlimit(100000)
G=json.load(open('pr2-analysis/data/graph.json')); T={t['name']:t for t in G['types']}
def top(t):
    while T[t]['nested']: t=T[t]['container']
    return t
def comp(t):
    f=T[t]['folder'].replace('\\','/'); return {'':'Core','Linq/JsonPath':'Linq.JsonPath'}.get(f,f)
LAYER={'Utilities':0,'Core':1,'Linq':2,'Bson':2,'Serialization':2,'Linq.JsonPath':3,'Converters':3,'Schema':3}
tops=[t for t in T if not T[t]['nested']]
E=set()
for e in G['edges']:
    a,b=top(e['from']),top(e['to'])
    if a!=b: E.add((a,b))
def scc(edges):
    adj=collections.defaultdict(list)
    for a,b in edges: adj[a].append(b)
    idx={};low={};st=[];on=set();res=[];i=[0]
    def sc(v):
        idx[v]=low[v]=i[0];i[0]+=1;st.append(v);on.add(v)
        for w in adj[v]:
            if w not in idx: sc(w);low[v]=min(low[v],low[w])
            elif w in on: low[v]=min(low[v],idx[w])
        if low[v]==idx[v]:
            s=[]
            while True:
                w=st.pop();on.discard(w);s.append(w)
                if w==v:break
            res.append(s)
    for v in tops:
        if v not in idx: sc(v)
    return [s for s in res if len(s)>1]
def report(name,edges):
    S=scc(edges); big=max(S,key=len) if S else []
    print(f"{name:45} SCCs={len(S)} largest={len(big)} in-cycles={sum(map(len,S))} comps={dict(collections.Counter(comp(t) for t in big))}")
report('as is',E)
viol={(a,b) for a,b in E if comp(a)!=comp(b) and LAYER[comp(a)]<=LAYER[comp(b)]}
print('violating type pairs',len(viol),'source types',len({a for a,b in viol}))
print(sorted({a.split('.')[-1] for a,b in viol}))
report('without layer violations',E-viol)
# which intra-package cycles remain
S=scc(E-viol)
for s in sorted(S,key=len,reverse=True)[:6]: print(len(s),comp(s[0]),[x.split('.')[-1] for x in s][:12])
