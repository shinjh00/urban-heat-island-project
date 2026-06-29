from abc import ABC, abstractmethod
import geopandas as gpd
import pandas as pd

class BaseWeatherService(ABC):
    """모든 데이터 소스 서비스의 골격이 되는 추상 클래스"""
    def __init__(self):
        self.geojson_path = "../data/seoul/mapo_boundary.geojson"
        self._load_mapo_boundary()

    def _load_mapo_boundary(self):
        """정적 마포구 맵 데이터를 서버 기동 시 캐싱"""
        self.mapo_dong_gdf = gpd.read_file(self.geojson_path)
        if self.mapo_dong_gdf.crs != "EPSG:4326":
            self.mapo_dong_gdf = self.mapo_dong_gdf.to_crs("EPSG:4326")
        self.mapo_all_polygon = self.mapo_dong_gdf.unary_union
        self.mapo_gdf = gpd.GeoDataFrame(geometry=[self.mapo_all_polygon], crs="EPSG:4326")

    @abstractmethod
    def generate_decal_image(self, tm, obs):
        """컨트롤러가 공통으로 호출할 비즈니스 로직 규격"""
        pass

