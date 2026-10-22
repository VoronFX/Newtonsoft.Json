import json,collections,statistics,sys
G=json.load(open('pr2-analysis/data/graph.json'))
T={t['name']:t for t in G['types']}
def comp(t):
    f=T[t]['folder'].replace('\\','/')
    return {'':'Core','Linq/JsonPath':'Linq.JsonPath'}.get(f,f)
def top(t):
    while T[t]['nested']: t=T[t]['container']
    return t
# type-level graph on top-level types (nested folded into container)
E=collections.defaultdict(lambda: collections.Counter())
for e in G['edges']:
    a,b=top(e['from']),top(e['to'])
    if a!=b and a in T and b in T: E[(a,b)][e['kind']]+=e['count']
tops=[t for t in T if not T[t]['nested']]
out=collections.defaultdict(set); inc=collections.defaultdict(set)
for (a,b) in E: out[a].add(b); inc[b].add(a)
# component-level
CE=collections.Counter(); CEk=collections.defaultdict(collections.Counter)
for (a,b),k in E.items():
    ca,cb=comp(a),comp(b)
    if ca!=cb: CE[(ca,cb)]+=1; CEk[(ca,cb)].update(k)
comps=sorted(set(comp(t) for t in tops))
print('components',comps)
for c in comps:
    ts=[t for t in tops if comp(t)==c]
    print(f"{c:15} types={len(ts):3} lines={sum(T[t]['lines'] for t in ts):6}  out->", {b:n for (a,b),n in CE.items() if a==c})
# SCC (Tarjan) on component graph
def scc(nodes,adj):
    idx={};low={};st=[];on=set();res=[];i=[0]
    sys.setrecursionlimit(100000)
    def sc(v):
        idx[v]=low[v]=i[0];i[0]+=1;st.append(v);on.add(v)
        for w in adj.get(v,()):
            if w not in idx: sc(w);low[v]=min(low[v],low[w])
            elif w in on: low[v]=min(low[v],idx[w])
        if low[v]==idx[v]:
            s=[]
            while True:
                w=st.pop();on.discard(w);s.append(w)
                if w==v:break
            res.append(s)
    for v in nodes:
        if v not in idx: sc(v)
    return res
cadj=collections.defaultdict(set)
for (a,b) in CE: cadj[a].add(b)
print('component SCCs',[s for s in scc(comps,cadj) if len(s)>1])
tadj={a:out[a] for a in out}
S=[s for s in scc(tops,tadj) if len(s)>1]
print('type SCCs sizes',sorted(len(s) for s in S)[::-1][:5], 'types in cycles',sum(len(s) for s in S),'of',len(tops))
big=max(S,key=len); print('largest SCC comps',collections.Counter(comp(t) for t in big))
# hubs
deg={t:(len(inc[t]),len(out[t])) for t in tops}
med_in=statistics.median(d[0] for d in deg.values()); med_out=statistics.median(d[1] for d in deg.values()); med_tot=statistics.median(sum(d) for d in deg.values())
print('median in/out/total',med_in,med_out,med_tot)
for t,(i,o) in sorted(deg.items(),key=lambda x:-(x[1][0]+x[1][1]))[:22]:
    print(f"{t.split('.')[-1]:34} {comp(t):13} in={i:3} out={o:3} tot={i+o:3} methods={T[t]['methods']} lines={T[t]['lines']} kind={T[t]['kind']} {T[t]['access']}")
json.dump(dict(comps=comps,CE={f"{a}->{b}":n for (a,b),n in CE.items()},deg={t:d for t,d in deg.items()},scc=S),open('pr2-analysis/data/agg.json','w'),indent=1)
