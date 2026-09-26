import json
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import numpy as np

plt.rcParams.update({
    'font.family': 'DejaVu Sans',
    'font.size': 9,
    'axes.spines.top': False,
    'axes.spines.right': False,
    'axes.edgecolor': '#9a9a9a',
    'axes.labelcolor': '#333',
    'xtick.color': '#444',
    'ytick.color': '#444',
})

s = json.load(open('results/sqale.json'))
m = json.load(open('results/metrics.json'))

files = [
    "JsonSerializerInternalReader.cs", "DefaultContractResolver.cs", "JsonSerializerInternalWriter.cs",
    "JsonTypeReflector.cs", "JsonContract.cs", "JsonSerializerInternalBase.cs", "JsonProperty.cs",
]
cats = ['Complexity', 'Duplication', 'Potential Defects', 'Design/Performance', 'Style']

# --- heatmap: debt density (per 100 LOC) by file and category ---
M = np.array([[s['matrix'].get(f, {}).get(c, 0) / s['loc'][f]['ncloc'] * 100 for c in cats] for f in files])
tot = np.array([sum(s['matrix'].get(f, {}).values()) / s['loc'][f]['ncloc'] * 100 for f in files])
data = np.column_stack([M, tot])

fig, ax = plt.subplots(figsize=(8.6, 3.9), dpi=200)
im = ax.imshow(data, cmap='Oranges', aspect='auto', vmin=0, vmax=data.max())

labs = cats + ['Total']
ax.set_xticks(range(len(labs)), [l.replace('/', '/\n').replace(' Defects', '\nDefects') for l in labs])
ax.set_yticks(range(len(files)), [f"{f.replace('.cs', '')}  ({s['loc'][f]['ncloc']} LOC)" for f in files])

for i in range(data.shape[0]):
    for j in range(data.shape[1]):
        v = data[i, j]
        ax.text(j, i, f"{v:.1f}" if v else "–", ha='center', va='center', fontsize=8.5,
                color='white' if v > data.max() * 0.55 else '#222')

ax.axvline(len(cats) - 0.5, color='white', lw=3)
ax.set_xticks(np.arange(-.5, data.shape[1]), minor=True)
ax.set_yticks(np.arange(-.5, data.shape[0]), minor=True)
ax.grid(which='minor', color='white', lw=2)
ax.tick_params(which='minor', length=0)
ax.tick_params(length=0)
for sp in ax.spines.values():
    sp.set_visible(False)

cb = fig.colorbar(im, ax=ax, fraction=0.03, pad=0.02)
cb.set_label('min of debt per 100 LOC')
cb.outline.set_visible(False)
ax.set_title('Technical debt density (SQALE) by file and category', loc='left', fontsize=10.5, color='#222')
fig.tight_layout()
fig.savefig('results/heatmap.png')
plt.close()

# --- top 20 methods by cyclomatic complexity ---
top = m['cc'][:20][::-1]
short = {'JsonSerializerInternalReader.cs': 'Reader', 'JsonSerializerInternalWriter.cs': 'Writer', 'DefaultContractResolver.cs': 'Resolver'}
names = [f"{x['name']}  [{short.get(x['file'], x['file'])}]" for x in top]
vals = [x['cc'] for x in top]

fig, ax = plt.subplots(figsize=(8.6, 5.4), dpi=200)
ax.barh(range(len(top)), vals, color='#2a78d6', height=0.62)
for i, v in enumerate(vals):
    ax.text(v + 0.6, i, str(v), va='center', fontsize=8, color='#333')
ax.set_yticks(range(len(top)), names, fontsize=8)

for t, lab in ((10, 'M = 10'), (20, 'M = 20')):
    ax.axvline(t, color='#666', lw=1, ls='--')
    ax.text(t, len(top) - 0.35, lab, fontsize=7.5, color='#555', va='bottom', ha='center', bbox=dict(fc='white', ec='none', pad=1))

ax.text(15, len(top) + 0.35, 'moderate', fontsize=7, color='#777', ha='center')
ax.text(40, len(top) + 0.35, 'high risk (refactoring candidates)', fontsize=7, color='#777', ha='center')
ax.text(5, len(top) + 0.35, 'low', fontsize=7, color='#777', ha='center')

ax.set_xlabel('Cyclomatic complexity M (Roslyn CA1502)')
ax.set_xlim(0, 62)
ax.set_ylim(-0.6, len(top) + 0.9)
ax.set_title('Top 20 methods by cyclomatic complexity', loc='left', fontsize=10.5, color='#222')
ax.grid(axis='x', color='#e6e6e6', lw=0.6)
ax.set_axisbelow(True)
fig.tight_layout()
fig.savefig('results/cc_top20.png')
plt.close()

# --- debt by category ---
bc = s['bycat']
order = sorted(cats, key=lambda c: bc.get(c, 0))
v = [bc.get(c, 0) / 60 for c in order]

fig, ax = plt.subplots(figsize=(8.6, 2.6), dpi=200)
ax.barh(order, v, color='#2a78d6', height=0.6)
for i, x in enumerate(v):
    ax.text(x + 0.5, i, f"{x:.1f} h ({x / sum(v) * 100:.0f}%)", va='center', fontsize=8)
ax.set_xlabel('Remediation cost, person-hours')
ax.set_xlim(0, max(v) * 1.3)
ax.grid(axis='x', color='#e6e6e6', lw=0.6)
ax.set_axisbelow(True)
ax.set_title('Technical debt breakdown by category', loc='left', fontsize=10.5, color='#222')
fig.tight_layout()
fig.savefig('results/debt_by_cat.png')
plt.close()
