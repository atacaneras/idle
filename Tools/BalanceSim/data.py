"""Tek kaynak: Assets/GameData/Content/content.json

Bu dosya artik icerigi tutmuyor; sadece JSON'u simulasyonun bekledigi
kolay formata ceviriyor. Oyun icerigi icin content.json'u duzenle.
"""
import json
import os

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CONTENT_PATH = os.path.join(ROOT, 'Assets', 'GameData', 'Content', 'content.json')

KIND_TO_SIM = {
    'ClickMultiplier': 'click',
    'ClickProductionShare': 'share',
    'BuildingMultiplier': 'bld',
    'GlobalMultiplier': 'glob',
}


def load_content(path=CONTENT_PATH):
    with open(path, encoding='utf-8') as f:
        return json.load(f)


def _build():
    data = load_content()
    resources = {r['id']: r for r in data['resources']}

    ages = []
    for a in data['ages']:
        buildings = []
        for b in a['buildings']:
            buildings.append((
                b['id'],
                b['displayName'],
                b['produces'],
                b['costResource'],
                b['baseCost'],
                b['baseProduction'],
                (b['scenePosition'][0], b['scenePosition'][1], b['sortingOrder']),
                tuple(b['placeholderColor']),
            ))
        ages.append(dict(
            id=a['id'],
            name=a['displayName'],
            click=a['clickResource'],
            advance=a['advanceResource'],
            advanceCost=a['advanceCost'],
            knowledge=a['knowledgeReward'],
            resources=a['resources'],
            buildings=buildings,
            upgrades=a['upgrades'],
        ))

    perm_index = {p['id']: i for i, p in enumerate(data['permanentUpgrades'])}
    perms = []
    for p in data['permanentUpgrades']:
        perms.append((
            p['id'],
            p['displayName'],
            p['description'],
            p['kind'],
            p['valuePerLevel'],
            p['maxLevel'],
            p['baseCost'],
            p['costGrowth'],
            perm_index.get(p['requires']) if p.get('requires') else None,
        ))

    game = data.get('game', {})
    return ages, perms, resources, game


AGES, PERMS, RESOURCES, GAME = _build()
BY_ID = {a['id']: a for a in AGES}

STONE = BY_ID['stone']
FARM = BY_ID['farm']
BRONZE = BY_ID['bronze']
ANCIENT = BY_ID['antique']
MEDIEVAL = BY_ID.get('medieval')


def upgrades_for(age):
    """Simulasyon icin duz yukseltme listesi (JSON'daki kayitlardan)."""
    ups = []
    for u in age['upgrades']:
        ups.append(dict(
            id=u['id'],
            name=u['displayName'],
            kind=KIND_TO_SIM[u['kind']],
            value=u['value'],
            res=u['costResource'],
            cost=u['cost'],
            desc=u['description'],
            pre=u['prerequisite'],
            unlock=(u['unlockBuilding'], u['unlockCount']) if u.get('unlockBuilding') else None,
            target=u['targetBuilding'],
        ))
    return ups


if __name__ == '__main__':
    print('content:', CONTENT_PATH)
    print('ages:', [a['id'] for a in AGES])
    print('resources:', len(RESOURCES), 'perms:', len(PERMS))
