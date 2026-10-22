import json
cc=json.load(open('pr2-analysis/data/cc_all.json')); fl=json.load(open('pr2-analysis/data/flow.json'))
F={(f['file'],f['line']):f for f in fl}
rows=[]
for c in cc:
    f=None
    for d in range(0,6):
        if (c['file'],c['line']-d) in F: f=F[(c['file'],c['line']-d)];break
    if f: rows.append({**c,**{k:f[k] for k in ('depth','gotos','returns','breaks','switches','loops','lines')}})
hi=[r for r in rows if r['cc']>20]
tangled=[r for r in hi if r['depth']>=4 and (r['gotos']>0 or r['breaks']>=5 or r['loops']>=3)]
flat=[r for r in hi if r['depth']<=3]
print('M>20:',len(hi),'tangled:',len(tangled),'flat(depth<=3):',len(flat),'other:',len(hi)-len(tangled)-len(flat))
for r in sorted(tangled,key=lambda r:-r['cc']): print(r['cc'],r['name'],r['file'],r['depth'],r['loops'],r['gotos'],r['breaks'],r['lines'])
print('--flat')
for r in sorted(flat,key=lambda r:-r['cc'])[:10]: print(r['cc'],r['name'],r['file'],r['depth'],r['switches'],r['returns'])
json.dump(dict(rows=rows,tangled=tangled,flat=flat),open('pr2-analysis/data/spaghetti.json','w'))
