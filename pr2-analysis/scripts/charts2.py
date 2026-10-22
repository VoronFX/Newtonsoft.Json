import json, matplotlib; matplotlib.use('Agg')
import matplotlib.pyplot as plt, numpy as np, statistics
plt.rcParams.update({'font.family':'DejaVu Sans','font.size':9,'axes.spines.top':False,'axes.spines.right':False,'axes.edgecolor':'#9a9a9a'})
S=json.load(open('pr2-analysis/data/spaghetti.json'))
rows=[r for r in S['rows'] if r['cc']>10]
tang={(r['file'],r['line']) for r in S['tangled']}
fig,ax=plt.subplots(figsize=(8.6,4.4),dpi=200)
rng=np.random.default_rng(1)
for r in rows:
    t=(r['file'],r['line']) in tang
    ax.scatter(r['cc'],r['depth']+rng.uniform(-0.18,0.18),s=22 if t else 12,color='#C92A2A' if t else '#2a78d6',alpha=0.85 if t else 0.45,edgecolors='white',linewidths=0.6,zorder=3 if t else 2)
ax.axvline(20,color='#666',ls='--',lw=1); ax.axhline(3.5,color='#666',ls=':',lw=1)
pos={'CreateObjectUsingCreatorWithParameters':(40,7.6),'WriteEscapedJavaScriptString':(41,6.45),'DecimalTryParse':(57.5,5.7),'ReadStringValue':(52.5,4.6),'SerializeNode':(42,5.55),'WriteValue':(61,3.75),'Operation':(62,1.3)}
done=set()
for r in rows:
    n=r['name']
    if n in pos and n not in done and r['cc']>30 and not n.endswith('Async'):
        done.add(n); x,y=pos[n]
        ax.annotate(n,(r['cc'],r['depth']),xytext=(x,y),fontsize=7,color='#333',ha='left',arrowprops=dict(arrowstyle='-',color='#999',lw=0.6))
ax.set_xlabel('Цикломатична складність M (Roslyn CA1502)'); ax.set_ylabel('Макс. глибина вкладеності керуючих конструкцій')
ax.text(20.4,0.3,'M > 20',fontsize=7.5,color='#555'); ax.text(72,3.62,'глибина ≥ 4',fontsize=7.5,color='#555',ha='right')
ax.scatter([],[],color='#C92A2A',s=22,label=f"«заплутані»: M > 20, глибина ≥ 4, goto / ≥ 5 break-continue / ≥ 3 цикли ({len(S['tangled'])})")
ax.scatter([],[],color='#2a78d6',alpha=0.6,s=12,label=f'інші методи з M > 10 ({len(rows)-len(S["tangled"])})')
ax.legend(loc='upper left',fontsize=7.5,frameon=True,facecolor='white',edgecolor='#ddd')
ax.set_title('Методи з M > 10 у всій бібліотеці: складність проти глибини вкладеності',loc='left',fontsize=10,color='#222')
ax.grid(color='#eee',lw=0.6); ax.set_axisbelow(True); ax.set_ylim(0,9.4)
fig.tight_layout(); fig.savefig('fig_spaghetti.png'); plt.close()

agg=json.load(open('pr2-analysis/data/agg.json'))
deg={k.split('.')[-1]:v for k,v in agg['deg'].items()}
med=statistics.median(i+o for i,o in deg.values())
top=sorted(deg.items(),key=lambda x:-(x[1][0]+x[1][1]))[:14][::-1]
fig,ax=plt.subplots(figsize=(8.6,4.3),dpi=200)
y=np.arange(len(top))
ca=[v[0] for _,v in top]; ce=[v[1] for _,v in top]
ax.barh(y-0.2,ca,height=0.38,color='#2a78d6',label='Ca — вхідні (від скількох типів залежать від класу)')
ax.barh(y+0.2,ce,height=0.38,color='#eb6834',label='Ce — вихідні (від скількох типів залежить клас)')
for i,(n,v) in enumerate(top): ax.text(max(v)+1,i,f"Σ {v[0]+v[1]} ({(v[0]+v[1])/med:.0f}× мед.)",va='center',fontsize=7.5,color='#333')
ax.set_yticks(y,[n for n,_ in top],fontsize=8)
ax.axvline(med,color='#666',ls='--',lw=1); ax.text(med+0.5,len(top)-0.5,f'медіана Ca+Ce = {med:.0f}',fontsize=7.5,color='#555')
ax.set_xlabel('Кількість типів бібліотеки'); ax.legend(loc='center right',fontsize=7.5,frameon=False); ax.set_xlim(0,95)
ax.set_title('14 типів із найбільшою кількістю зв’язків (275 типів верхнього рівня)',loc='left',fontsize=10,color='#222')
ax.grid(axis='x',color='#eee',lw=0.6); ax.set_axisbelow(True)
fig.tight_layout(); fig.savefig('fig_hubs.png'); plt.close()
print(med)
