import hashlib, json, os, re

ROOT = r'C:\Users\Ata\idle'
ASSETS = os.path.join(ROOT, 'Assets', 'GameData')
CONTENT = os.path.join(ASSETS, 'Content', 'content.json')

SCRIPT_GUID = {
    'ResourceData': '126dae8d113fb184ab0fbed20ad9821c',
    'BuildingData': '463226b404ca49f41af037aa00f72ca2',
    'UpgradeData': '6c007ee57b681de42bcfae6ee007429e',
    'AgeData': '52ab2869040e360428780ad47804f328',
    'PermanentUpgradeData': '9d565ce15a45e334c825cc7e0f0787a8',
}
KIND_VALUE = {'ClickMultiplier': 0, 'ClickProductionShare': 1, 'BuildingMultiplier': 2, 'GlobalMultiplier': 3}


def q(s):
    return json.dumps(s, ensure_ascii=True)


def folder_meta(path):
    if not os.path.exists(path):
        os.makedirs(path, exist_ok=True)
    meta = path + '.meta'
    if os.path.exists(meta):
        return
    guid = hashlib.md5(path.encode('utf-8')).hexdigest()
    with open(meta, 'w', encoding='utf-8') as f:
        f.write('fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n' % guid)


def asset(path, kind, body):
    full = os.path.join(ROOT, path + '.asset')
    meta = full + '.meta'
    guid = None
    if os.path.exists(meta):
        m = re.search(r'^guid: ([0-9a-f]+)', open(meta, encoding='utf-8').read(), re.M)
        guid = m.group(1) if m else None
    if guid is None:
        guid = hashlib.md5(path.encode('utf-8')).hexdigest()
        with open(meta, 'w', encoding='utf-8') as f:
            f.write('fileFormatVersion: 2\nguid: %s\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n' % guid)
    if os.path.exists(full):
        return guid
    name = os.path.splitext(os.path.basename(path))[0]
    text = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n'
    text += '  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n'
    text += '  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n'
    text += '  m_Script: {fileID: 11500000, guid: %s, type: 3}\n' % SCRIPT_GUID[kind]
    text += '  m_Name: %s\n  m_EditorClassIdentifier: Assembly-CSharp::%s\n' % (name, kind)
    text += body
    with open(full, 'w', encoding='utf-8') as f:
        f.write(text)
    return guid


def ref(guid, obj_type=2):
    return '{fileID: 11400000, guid: %s, type: %d}' % (guid, obj_type)


data = json.load(open(CONTENT, encoding='utf-8'))
folder_meta(os.path.join(ASSETS, 'Content'))

# Tum varliklar icin guid haritasi
res_guid = {}
for r in data['resources']:
    res_guid[r['id']] = asset('Assets/GameData/Kaynaklar/' + r['asset'], 'ResourceData',
        '  id: %s\n  displayName: %s\n  icon: {fileID: 0}\n' % (r['id'], q(r['displayName'])))

bld_guid = {}
for age in data['ages']:
    for b in age['buildings']:
        body = '  id: %s\n  displayName: %s\n  icon: {fileID: 0}\n' % (b['id'], q(b['displayName']))
        body += '  produces: %s\n  costResource: %s\n' % (ref(res_guid[b['produces']]), ref(res_guid[b['costResource']]))
        body += '  baseCost: %s\n  costGrowth: %s\n  baseProduction: %s\n' % (b['baseCost'], b['costGrowth'], b['baseProduction'])
        body += '  levelSprites: []\n'
        body += '  scenePosition: {x: %s, y: %s}\n' % (b['scenePosition'][0], b['scenePosition'][1])
        body += '  sortingOrder: %s\n' % b['sortingOrder']
        body += '  placeholderColor: {r: %s, g: %s, b: %s, a: 1}\n' % tuple(b['placeholderColor'])
        body += '  placeholderSize: {x: %s, y: %s}\n' % tuple(b['placeholderSize'])
        bld_guid[b['id']] = asset('Assets/GameData/Binalar/' + b['asset'], 'BuildingData', body)

upg_guid = {}
for age in data['ages']:
    for u in age['upgrades']:
        body = '  id: %s\n  displayName: %s\n  description: %s\n  icon: {fileID: 0}\n' % (
            u['id'], q(u['displayName']), q(u['description']))
        body += '  kind: %s\n' % KIND_VALUE[u['kind']]
        body += '  targetBuilding: %s\n' % (ref(bld_guid[u['targetBuilding']]) if u['targetBuilding'] else '{fileID: 0}')
        body += '  value: %s\n' % u['value']
        body += '  costResource: %s\n  cost: %s\n' % (ref(res_guid[u['costResource']]), u['cost'])
        body += '  unlockBuilding: %s\n  unlockCount: %s\n' % (
            ref(bld_guid[u['unlockBuilding']]) if u['unlockBuilding'] else '{fileID: 0}', u['unlockCount'])
        body += '  prerequisite: %s\n' % (ref(upg_guid[u['prerequisite']]) if u['prerequisite'] else '{fileID: 0}')
        upg_guid[u['id']] = asset('Assets/GameData/Yukseltmeler/' + u['asset'], 'UpgradeData', body)

for age in data['ages']:
    body = '  id: %s\n  displayName: %s\n' % (age['id'], q(age['displayName']))
    body += '  resources:\n' + ''.join('  - %s\n' % ref(res_guid[x]) for x in age['resources'])
    body += '  buildings:\n' + ''.join('  - %s\n' % ref(bld_guid[b['id']]) for b in age['buildings'])
    body += '  upgrades:\n' + ''.join('  - %s\n' % ref(upg_guid[u['id']]) for u in age['upgrades'])
    body += '  clickResource: %s\n  advanceResource: %s\n' % (ref(res_guid[age['clickResource']]), ref(res_guid[age['advanceResource']]))
    body += '  advanceCost: %s\n  knowledgeReward: %s\n' % (age['advanceCost'], age['knowledgeReward'])
    body += '  skyColor: {r: %s, g: %s, b: %s, a: 1}\n' % tuple(age['skyColor'])
    body += '  groundColor: {r: %s, g: %s, b: %s, a: 1}\n' % tuple(age['groundColor'])
    age['_guid'] = asset('Assets/GameData/Caglar/' + age['asset'], 'AgeData', body)

# JSON'a yeni guid'leri yaz (editor script zaten asset adiyla buluyor; bu sadece kontrol icin)
print('medieval age guid:', age['_guid'])
print('new resources:', [x for x in ['res_wool', 'res_cloth', 'res_iron', 'res_faith'] if os.path.exists(os.path.join(ASSETS, 'Kaynaklar', x + '.asset'))])
print('new buildings:', len([1 for b in data['ages'][-1]['buildings'] if os.path.exists(os.path.join(ASSETS, 'Binalar', b['asset'] + '.asset'))]))
print('new upgrades:', len([1 for u in data['ages'][-1]['upgrades'] if os.path.exists(os.path.join(ASSETS, 'Yukseltmeler', u['asset'] + '.asset'))]))
