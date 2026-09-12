"""Generate reproducible town data before any Blender geometry is touched."""
import random
from .config import snap
from .model import Town, WaterBody
from .districts import generate_districts
from .buildings import place_buildings
from .paths import generate_paths, point_in_water


def generate_town(config):
    config.validate()
    rng=random.Random(config.seed)
    s=config.town_size
    q=lambda xy:tuple(snap(v,config.voxel_size) for v in xy)
    shore_y=rng.uniform(-.07,.07)*s
    water=[]
    if config.water_amount>0:
        water.append(WaterBody('oasis-0',q((-.35*s,shore_y)),
                               q((s*(.11+.11*config.water_amount),s*(.29+.12*config.water_amount)))))
    anchors={'plaza':q((s*rng.uniform(.025,.08),s*rng.uniform(-.035,.035))),
             'main_entrance':q((s*rng.uniform(.14,.27),-.435*s)),
             'secondary_entrance':q((.435*s,s*rng.uniform(.12,.28))),
             'market':q((s*rng.uniform(.14,.23),s*rng.uniform(-.24,-.18))),
             'water':q((-.095*s,shore_y-.035*s)),
             'farming':q((s*rng.uniform(-.065,-.015),s*rng.uniform(.20,.29))),
             'ruin':q((s*rng.uniform(.24,.37),s*rng.uniform(.31,.39)))}
    # The well/shore gathering point remains dry even at the largest oasis radius.
    while any(point_in_water(anchors['water'],w,padding=3) for w in water):
        anchors['water']=q((anchors['water'][0]+config.voxel_size,anchors['water'][1]))
    districts=generate_districts(config,anchors,rng)
    buildings,farms=place_buildings(config,anchors,districts,water,rng)
    paths=generate_paths(config,buildings,water,anchors)
    return Town(config,buildings,water,districts,paths,farms,anchors)
