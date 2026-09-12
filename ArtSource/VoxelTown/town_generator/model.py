"""Semantic source of truth. This module deliberately has no Blender dependency."""
from dataclasses import dataclass, field, asdict
from .config import Config


@dataclass
class Room:
    id: str
    role: str
    center: tuple
    width: float
    depth: float


@dataclass
class Building:
    id: str
    role: str
    position: tuple
    rotation: float
    width: float
    depth: float
    wall_height: float
    entrances: list
    rooms: list
    material_set: str
    prop_profile: str
    occupants: list


@dataclass
class Path:
    id: str
    points: list
    width: float
    purpose: str


@dataclass
class WaterBody:
    id: str
    center: tuple
    radii: tuple


@dataclass
class FarmPlot:
    id: str
    building_id: str
    center: tuple
    width: float
    depth: float


@dataclass
class District:
    id: str
    role: str
    center: tuple
    radius: float


@dataclass
class Town:
    config: Config
    buildings: list
    water_bodies: list
    districts: list
    paths: list
    farms: list
    anchors: dict
    props: list = field(default_factory=list)
    npcs: list = field(default_factory=list)


def to_dict(town):
    """Return an independent JSON-serializable snapshot, including semantic IDs."""
    return asdict(town)
