"""Districts are influence centers, never rectangular zoning boundaries."""
from .model import District
from .config import snap

ROLE_DISTRICT = {'residence':'residential','workshop':'industrial','trader':'commercial',
                 'meeting_house':'civic','shrine':'religious','storehouse':'industrial',
                 'farmhouse':'agricultural','bathhouse':'water','animal_enclosure':'agricultural',
                 'guard_watch':'defensive'}


def generate_districts(config, anchors, rng):
    s=config.town_size
    specs=(('civic',anchors['plaza'],.13),('residential',(.20*s,.12*s),.19),
           ('agricultural',anchors['farming'],.16),('commercial',anchors['market'],.15),
           ('industrial',(.01*s,-.23*s),.16),('water',anchors['water'],.12),
           ('religious',(.24*s,.28*s),.12),('defensive',anchors['main_entrance'],.12))
    return [District('district-'+role,role,
                     tuple(snap(v+rng.uniform(-s*.025,s*.025),config.voxel_size) for v in center),s*radius)
            for role,center,radius in specs]
