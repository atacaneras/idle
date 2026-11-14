"""Icerik dogrulama araci.

Kullanim (repo kokunden):
    python Tools/BalanceSim/validate_content.py

JSON (Assets/GameData/Content/content.json), uretilen ScriptableObject asset'leri
ve sahnedeki GameManager baglantilarini karsilikli kontrol eder. Hata varsa
cikis kodu 1 olur; CI'da dogrudan kullanilabilir.
"""
import glob
import json
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
ASSETS = os.path.join(ROOT, 'Assets', 'GameData')
CONTENT = os.path.join(ASSETS, 'Content', 'content.json')
SCENE = os.path.join(ROOT, 'Assets', 'Scenes', 'SampleScene.unity')

errors = []
warnings = []


def err(msg):
    errors.append(msg)


def warn(msg):
    warnings.append(msg)


def read(path):
    with open(path, encoding='utf-8') as f:
        return f.read()


def field(text, name):
    m = re.search(r'^  ' + re.escape(name) + r': (.*)$', text, re.M)
    return m.group(1).strip() if m else None


def unquote(value):
    if value is None:
        return None
    value = value.strip()
    if value.startswith('"') and value.endswith('"'):
        inner = value[1:-1]
        inner = re.sub(r'\\u([0-9a-fA-F]{4})', lambda m: chr(int(m.group(1), 16)), inner)
        inner = re.sub(r'\\x([0-9a-fA-F]{2})', lambda m: chr(int(m.group(1), 16)), inner)
        inner = inner.replace('\\"', '"').replace('\\\\', '\\')
        return inner
    return value


def section_guids(text, name):
    m = re.search(r'^  ' + re.escape(name) + r':\n((?:  - .*\n)*)', text, re.M)
    return re.findall(r'guid: ([0-9a-f]+)', m.group(1)) if m else []


def vec(text, name):
    m = re.search(r'^  ' + re.escape(name) + r': \{x: ([^,]+), y: ([^}]+)\}', text, re.M)
    return (float(m.group(1)), float(m.group(2))) if m else None


# ---- Asset haritasi ----
assets_by_guid = {}
assets_by_folder = {}


def load_assets():
    for path in glob.glob(os.path.join(ASSETS, '**', '*.asset'), recursive=True):
        meta = path + '.meta'
        if not os.path.exists(meta):
            err('meta yok: ' + os.path.relpath(path, ROOT))
            continue
        m = re.search(r'^guid: ([0-9a-f]+)', read(meta), re.M)
        if not m:
            err('meta icinde guid yok: ' + os.path.relpath(meta, ROOT))
            continue
        text = read(path)
        if not text.startswith('%YAML'):
            err('YAML basligi bozuk: ' + os.path.relpath(path, ROOT))
        info = {
            'path': path,
            'name': os.path.splitext(os.path.basename(path))[0],
            'guid': m.group(1),
            'id': field(text, 'id'),
            'text': text,
            'folder': os.path.basename(os.path.dirname(path)),
        }
        assets_by_guid[info['guid']] = info
        assets_by_folder.setdefault(info['folder'], {})[info['name']] = info


def guid_id(guid):
    info = assets_by_guid.get(guid)
    return info['id'] if info else None


def check_guids():
    for info in assets_by_guid.values():
        for line in info['text'].splitlines():
            if 'guid:' not in line:
                continue
            # m_Script referanslari .cs dosyalarina gider; asset referansi degildir.
            if line.lstrip().startswith('m_Script:'):
                continue
            for g in re.findall(r'guid: ([0-9a-f]{32})', line):
                if g not in assets_by_guid:
                    err('kirik referans %s -> %s' % (os.path.relpath(info['path'], ROOT), g))


def data():
    if not os.path.exists(CONTENT):
        err('content.json yok: ' + CONTENT)
        return None
    try:
        return json.loads(read(CONTENT))
    except Exception as e:
        err('content.json okunamadi: ' + str(e))
        return None


def check_unique(ids, label):
    seen = set()
    for i in ids:
        if i in seen:
            err('%s id tekrar ediyor: %s' % (label, i))
        seen.add(i)


def check_asset_shape(entry, folder, label):
    name = entry.get('asset')
    if not name:
        err('%s: asset alani bos (%s)' % (label, entry.get('id')))
        return None
    info = assets_by_folder.get(folder, {}).get(name)
    if info is None:
        err('%s: asset bulunamadi %s/%s.asset' % (label, folder, name))
        return None
    if info['id'] != entry.get('id'):
        err('%s: id uyusmuyor asset=%s json=%s (%s)' % (label, info['id'], entry.get('id'), name))
    return info


def check_age(age, resources):
    label = 'cag ' + age.get('id', '?')
    info = check_asset_shape(age, 'Caglar', label)
    if info is None:
        return
    text = info['text']

    cost = field(text, 'advanceCost')
    if cost is None:
        err(label + ': advanceCost yok')
    elif abs(float(cost) - float(age.get('advanceCost', 0))) > 1e-6:
        err('%s: advanceCost asset=%s json=%s' % (label, cost, age.get('advanceCost')))
    if float(age.get('advanceCost', 0)) <= 0:
        err(label + ': advanceCost sifir/negatif')

    reward = field(text, 'knowledgeReward')
    if reward is not None and float(reward) != float(age.get('knowledgeReward', 0)):
        err('%s: knowledgeReward asset=%s json=%s' % (label, reward, age.get('knowledgeReward')))

    age_res = age.get('resources') or []
    check_unique(age_res, label + ' kaynak')
    for rid in age_res:
        if rid not in resources:
            err('%s: tanimsiz kaynak %s' % (label, rid))
    if age.get('clickResource') not in age_res:
        err(label + ': clickResource cag kaynaklarinda yok')
    if age.get('advanceResource') not in age_res:
        err(label + ': advanceResource cag kaynaklarinda yok')

    if [guid_id(g) for g in section_guids(text, 'resources')] != age_res:
        err(label + ': asset resources listesi JSON ile ayni degil')
    if len(section_guids(text, 'buildings')) != len(age.get('buildings') or []):
        err(label + ': bina sayisi uyusmuyor')
    if len(section_guids(text, 'upgrades')) != len(age.get('upgrades') or []):
        err(label + ': yukseltme sayisi uyusmuyor')

    click = field(text, 'clickResource')
    if click:
        g = re.search(r'guid: ([0-9a-f]+)', click)
        if g and guid_id(g.group(1)) != age.get('clickResource'):
            err(label + ': clickResource asset referansi yanlis')

    for b in age.get('buildings') or []:
        check_building(b, label, age_res)
    for u in age.get('upgrades') or []:
        check_upgrade(u, label, age_res)


def check_building(b, age_label, age_res):
    label = age_label + ' / bina ' + b.get('id', '?')
    info = check_asset_shape(b, 'Binalar', label)
    if info is None:
        return
    text = info['text']
    for key in ('produces', 'costResource'):
        g = re.search(r'guid: ([0-9a-f]+)', field(text, key) or '')
        if not g or guid_id(g.group(1)) != b.get(key):
            err('%s: %s referansi yanlis' % (label, key))
    if b.get('produces') not in age_res:
        err('%s: produces cag kaynaklarinda yok' % label)
    if b.get('costResource') not in age_res:
        err('%s: costResource cag kaynaklarinda yok' % label)
    for key in ('baseCost', 'baseProduction'):
        v = field(text, key)
        if v is None or abs(float(v) - float(b.get(key, 0))) > 1e-6:
            err('%s: %s asset=%s json=%s' % (label, key, v, b.get(key)))
    if float(b.get('baseCost', 0)) <= 0:
        err(label + ': baseCost sifir/negatif')
    if float(b.get('baseProduction', 0)) <= 0:
        err(label + ': baseProduction sifir/negatif')
    if vec(text, 'scenePosition') is None:
        err(label + ': scenePosition yok')


def check_upgrade(u, age_label, age_res):
    label = age_label + ' / yukseltme ' + u.get('id', '?')
    info = check_asset_shape(u, 'Yukseltmeler', label)
    if info is None:
        return
    text = info['text']
    if u.get('costResource') not in age_res:
        err('%s: costResource cag kaynaklarinda yok' % label)

    g = re.search(r'guid: ([0-9a-f]+)', field(text, 'costResource') or '')
    if not g or guid_id(g.group(1)) != u.get('costResource'):
        err('%s: costResource referansi yanlis' % label)

    if float(u.get('cost', 0)) <= 0:
        err(label + ': cost sifir/negatif')

    for key, target_key in (('targetBuilding', 'targetBuilding'), ('unlockBuilding', 'unlockBuilding')):
        expected = u.get(target_key)
        g = re.search(r'guid: ([0-9a-f]+)', field(text, key) or '')
        actual = guid_id(g.group(1)) if g else None
        if actual != expected:
            err('%s: %s asset=%s json=%s' % (label, key, actual, expected))

    expected_pre = u.get('prerequisite')
    g = re.search(r'guid: ([0-9a-f]+)', field(text, 'prerequisite') or '')
    actual_pre = guid_id(g.group(1)) if g else None
    if actual_pre != expected_pre:
        err('%s: prerequisite asset=%s json=%s' % (label, actual_pre, expected_pre))


def check_scene(content):
    if not os.path.exists(SCENE):
        err('sahne bulunamadi: ' + SCENE)
        return
    text = read(SCENE)
    m = re.search(r'^  ages:\n((?:  - .*\n)*)', text, re.M)
    if not m:
        err('sahnede GameManager.ages bulunamadi')
        return
    scene_guids = re.findall(r'guid: ([0-9a-f]+)', m.group(1))
    expected = []
    for age in content.get('ages') or []:
        info = assets_by_folder.get('Caglar', {}).get(age.get('asset'))
        if info:
            expected.append(info['guid'])
    if scene_guids != expected:
        err('sahne ages listesi JSON sirasiyla uyusmuyor')
        err('  sahne:   ' + str([guid_id(g) for g in scene_guids]))
        err('  beklenen:' + str([age.get('id') for age in content.get('ages') or []]))

    m = re.search(r'^  permanentUpgrades:\n((?:  - .*\n)*)', text, re.M)
    if m:
        found = [guid_id(g) for g in re.findall(r'guid: ([0-9a-f]+)', m.group(1))]
        for pid in found:
            if pid is None:
                err('sahnede tanimsiz kalici yukseltme referansi')
        if len(found) != len(content.get('permanentUpgrades') or []):
            err('sahne kalici yukseltme sayisi JSON ile uyusmuyor')
    else:
        warn('sahnede permanentUpgrades bulunamadi (bos olabilir)')


def main():
    load_assets()
    check_guids()
    content = data()
    if content is None:
        report()
        return

    resources = {}
    for r in content.get('resources') or []:
        label = 'kaynak ' + r.get('id', '?')
        info = check_asset_shape(r, 'Kaynaklar', label)
        resources[r.get('id')] = r
        if info is not None and unquote(field(info['text'], 'displayName')) != r.get('displayName'):
            err('%s: displayName asset != json' % label)

    check_unique([r.get('id') for r in content.get('resources') or []], 'kaynak')
    check_unique([a.get('id') for a in content.get('ages') or []], 'cag')

    all_buildings = []
    all_upgrades = []
    for age in content.get('ages') or []:
        check_age(age, resources)
        all_buildings.extend(b.get('id') for b in age.get('buildings') or [])
        all_upgrades.extend(u.get('id') for u in age.get('upgrades') or [])
    check_unique(all_buildings, 'bina')
    check_unique(all_upgrades, 'yukseltme')

    perm_ids = set()
    for p in content.get('permanentUpgrades') or []:
        label = 'kalici ' + p.get('id', '?')
        check_asset_shape(p, 'Kalici', label)
        perm_ids.add(p.get('id'))
    for p in content.get('permanentUpgrades') or []:
        req = p.get('requires')
        if req and req not in perm_ids:
            err('kalici %s: tanimsiz onkosul %s' % (p.get('id'), req))
        if p.get('maxLevel', 0) <= 0:
            err('kalici %s: maxLevel sifir/negatif' % p.get('id'))
        if p.get('baseCost', 0) <= 0:
            err('kalici %s: baseCost sifir/negatif' % p.get('id'))

    check_scene(content)
    report()


def report():
    print('Icerik: %d cag, %d kaynak, %d bina, %d yukseltme' % (
        len(glob.glob(os.path.join(ASSETS, 'Caglar', '*.asset'))),
        len(glob.glob(os.path.join(ASSETS, 'Kaynaklar', '*.asset'))),
        len(glob.glob(os.path.join(ASSETS, 'Binalar', '*.asset'))),
        len(glob.glob(os.path.join(ASSETS, 'Yukseltmeler', '*.asset'))),
    ))
    for w in warnings:
        print('UYARI: ' + w)
    if errors:
        print('\n%d HATA:' % len(errors))
        for e in errors:
            print(' - ' + e)
        sys.exit(1)
    print('Dogrulama basarili: hata yok.')


if __name__ == '__main__':
    main()
