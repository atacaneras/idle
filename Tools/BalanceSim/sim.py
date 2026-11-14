import math, sys
from data import *
MIL=[25,50,100,200,300,400,500]
def run(age, upgrades, clicks_per_sec=0.7, growth=1.15, maxt=16*3600, verbose=False, step=5):
    res={}; cnt={b[0]:0 for b in age['buildings']}; bought=set(); t=0
    B={b[0]:b for b in age['buildings']}
    def get(r): return res.get(r,0)
    def bmult(bid):
        m=2**sum(1 for x in MIL if cnt[bid]>=x)
        for u in upgrades:
            if u['id'] in bought and u['kind']=='bld' and u['target']==bid: m*=u['value']
        return m
    def gmult():
        m=1
        for u in upgrades:
            if u['id'] in bought and u['kind']=='glob': m*=u['value']
        return m
    def clickv():
        v=1
        for u in upgrades:
            if u['id'] in bought and u['kind']=='click': v*=u['value']
        v*=gmult()
        sh=sum(u['value'] for u in upgrades if u['id'] in bought and u['kind']=='share')
        if sh: v+=sh*prod().get(age['click'],0)
        return v
    def prod():
        p={}
        for b in age['buildings']:
            n=cnt[b[0]]
            p[b[2]]=p.get(b[2],0)+n*b[5]*bmult(b[0])*gmult()
        return p
    def total(): return sum(prod().values())+clickv()*clicks_per_sec
    def cost(b,n=None): return b[4]*growth**(cnt[b[0]] if n is None else n)
    marks=[]
    while t<maxt:
        if get(age['advance'])>=age['advanceCost']: return t,cnt
        # buy loop
        while True:
            base=total(); best=None;bs=0
            for b in age['buildings']:
                c=cost(b)
                if get(b[3])<c: continue
                cnt[b[0]]+=1; g=total()-base; cnt[b[0]]-=1
                s=g/c
                if s>bs: bs=s;best=('b',b,c)
            for u in upgrades:
                if u['id'] in bought: continue
                if u.get('unlock') and cnt[u['unlock'][0]]<u['unlock'][1]: continue
                if u.get('pre') and u['pre'] not in bought: continue
                if get(u['res'])<u['cost']: continue
                bought.add(u['id']); g=total()-base; bought.discard(u['id'])
                s=g/u['cost']
                if s>bs: bs=s;best=('u',u,u['cost'])
            # advance resource: don't spend stone below target? keep simple
            if not best or bs<=0: break
            if best[0]=='b': res[best[1][3]]-=best[2]; cnt[best[1][0]]+=1
            else: res[best[1]['res']]-=best[2]; bought.add(best[1]['id'])
        p=prod(); cv=clickv()
        for r,v in p.items(): res[r]=get(r)+v*step
        res[age['click']]=get(age['click'])+cv*clicks_per_sec*step
        t+=step
    return None,cnt
if __name__=='__main__':
    t,cnt=run(STONE,[])
    print('no upgrades',t and t/60,cnt)
