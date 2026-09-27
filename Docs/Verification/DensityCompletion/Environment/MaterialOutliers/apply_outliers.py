from pathlib import Path
import json,hashlib,sys
root=Path(sys.argv[1]);out=Path(sys.argv[2]);out.mkdir(parents=True,exist_ok=True)
p=root/'Assets/Resources/Content/Blueprints/Objects.json';before=p.read_text();old=json.loads(before);text=before
changes=[('TarSeep','Volatility','30','0.3'),('PeatBog','Porosity','80','0.8'),('SteamVent','Porosity','100','1'),('FrostVent','Brittleness','70','0.7'),('IceSheet','Brittleness','90','0.9'),('TarSeep','MaterialTagsRaw','Liquid,Tar,Fuel','Liquid,Tar,Fuel,Flammable')]
import re
for name,key,a,b in changes:
 at=text.index('"Name": "'+name+'"');start=text.rfind('\n    {',0,at)+5
 obj,size=json.JSONDecoder().raw_decode(text[start:]);assert obj['Name']==name
 block=text[start:start+size];pattern=r'("Key": "'+key+r'",\s*"Value": ")'+re.escape(a)+r'(")'
 block,n=re.subn(pattern,lambda m:m[1]+b+m[2],block);assert n==1,(name,n)
 text=text[:start]+block+text[start+size:]
after=json.loads(text);expected=json.loads(before)
for name,key,a,b in changes:
 obj=next(o for o in expected['Objects'] if o['Name']==name);part=next(p for p in obj['Parts'] if p['Name']=='Material');param=next(p for p in part['Params'] if p['Key']==key);assert param['Value']==a;param['Value']=b
assert expected==after
p.write_text(text)
(out/'parsed-diff.json').write_text(json.dumps({'beforeSha256':hashlib.sha256(before.encode()).hexdigest(),'afterSha256':hashlib.sha256(text.encode()).hexdigest(),'changedTokens':changes,'unchangedBlueprints':len(old['Objects'])-len({c[0] for c in changes}),'allOtherParsedFieldsUnchanged':True},indent=2)+'\n')
