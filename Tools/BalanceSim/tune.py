from data import *; from sim import run
def tune(age,target_min,cps=0.5):
    ups=upgrades_for(age); lo,hi=age['advanceCost']/10,age['advanceCost']*1e4
    for _ in range(40):
        mid=(lo*hi)**0.5; age['advanceCost']=mid
        t,_c=run(age,ups,clicks_per_sec=cps,maxt=12*3600)
        if t is not None and t/60<target_min: lo=mid
        else: hi=mid
    age['advanceCost']=lo
    t,c=run(age,ups,clicks_per_sec=cps,maxt=12*3600)
    return lo,t/60,c
for a,tm in ((STONE,50),(FARM,100)):
    print(a['id'],tune(a,tm))
