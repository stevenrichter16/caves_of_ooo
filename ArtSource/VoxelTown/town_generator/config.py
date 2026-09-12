"""Validated inputs for the Blender-independent semantic generator."""
from dataclasses import dataclass
import math

ARCHETYPES = ('residence','workshop','trader','meeting_house','shrine',
              'storehouse','farmhouse','bathhouse','animal_enclosure','guard_watch')


@dataclass(frozen=True)
class Config:
    seed: int = 41
    town_size: float = 80
    building_count: int = 10
    population: int = 18
    density: float = .55
    water_amount: float = .5
    vegetation_amount: float = .65
    agriculture_amount: float = .5
    ruin_amount: float = .3
    wealth: float = .45
    town_age: float = .6
    clutter_amount: float = .6
    building_irregularity: float = .6
    path_irregularity: float = .4
    voxel_size: float = .25
    archetypes: tuple = ('residence','workshop','trader','farmhouse')

    def validate(self):
        for name in ('seed','building_count','population'):
            value=getattr(self,name)
            if isinstance(value,bool) or not isinstance(value,int):
                raise ValueError(f'{name} must be an integer')
        if not 0 <= self.building_count <= 256:
            raise ValueError('building_count must be in [0,256]')
        if not 0 <= self.population <= 4096:
            raise ValueError('population must be in [0,4096]')
        if not self.building_count and self.population:
            raise ValueError('population requires at least one building')
        if isinstance(self.town_size,bool) or not isinstance(self.town_size,(int,float)) or not math.isfinite(self.town_size) or not 32 <= self.town_size <= 512:
            raise ValueError('town_size must be finite and in [32,512]')
        if abs(self.town_size/.5-round(self.town_size/.5)) > 1e-7:
            raise ValueError('town_size must be a multiple of .5 meters so centered boundaries lie on the voxel grid')
        if self.building_count*85 > self.town_size**2*.72:
            raise ValueError('building_count exceeds this map size; enlarge town_size')
        for name in ('density','water_amount','vegetation_amount','agriculture_amount','ruin_amount',
                     'wealth','town_age','clutter_amount','building_irregularity','path_irregularity'):
            value=getattr(self,name)
            if isinstance(value,bool) or not isinstance(value,(int,float)) or not math.isfinite(value) or not 0 <= value <= 1:
                raise ValueError(f'{name} must be finite and in [0,1]')
        if isinstance(self.voxel_size,bool) or self.voxel_size != .25:
            raise ValueError('Milestone 1 towns require voxel_size=.25 meters')
        if not isinstance(self.archetypes,(tuple,list)) or not self.archetypes:
            raise ValueError('archetypes must be a nonempty sequence')
        if any(role not in ARCHETYPES for role in self.archetypes) or len(set(self.archetypes)) != len(self.archetypes):
            raise ValueError('archetypes must contain distinct supported roles')
        return self


def snap(value, step):
    """Quantize stable generated geometry to the shared voxel lattice."""
    return round(value/step)*step
