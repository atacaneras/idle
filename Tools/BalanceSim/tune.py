from data import *; from sim import run

def tune(age,target_min,cps=0.5):
    ups=upgrades_for(age); lo,hi=age['advanceCost']/10,age['advanceCost']*1e4
    for _ in range(40):
        mid=(lo*hi)**0.5; age['advanceCost']=mid
        t,_c=run(age,ups,clicks_per_sec=cps,maxt=16*3600)
        if t is not None and t/60<target_min: lo=mid
        else: hi=mid
    age['advanceCost']=lo
    t,c=run(age,ups,clicks_per_sec=cps,maxt=16*3600)
    return lo,(t/60 if t is not None else None),c

# Ilk tur hedefleri (dakika). Maliyetleri content.json'a elle aktar.
TARGET_MINUTES={"stone":45,"farm":90,"bronze":175,"antique":210,"medieval":200}
for age in AGES:
    current=age['advanceCost']
    current_time,_=run(age,upgrades_for(age),clicks_per_sec=0.5,maxt=16*3600)
    tuned_cost,tuned_time,_counts=tune(age,TARGET_MINUTES[age['id']],cps=0.5)
    measured=round(current_time/60,1) if current_time is not None else ">960 dk"
    tuned=round(tuned_time,1) if tuned_time is not None else ">960 dk"
    print("{}: mevcut maliyet={}; mevcut süre={}; hedef={} dk; önerilen maliyet={}; ölçülen süre={} dk".format(
        age['id'],current,measured,TARGET_MINUTES[age['id']],round(tuned_cost),tuned))
