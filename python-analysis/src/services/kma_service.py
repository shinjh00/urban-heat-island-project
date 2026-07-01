import struct
import json
import os
import requests
import numpy as np
import pandas as pd
import netCDF4 as nc
from flask import jsonify
from .base_service import BaseWeatherService
from .visualizer import DecalVisualizer


class KmaWeatherService(BaseWeatherService):
    """기상청 격자 데이터 전용 서비스 레이어"""
    def __init__(self):
        super().__init__()
        self.api_url = "https://apihub.kma.go.kr/api/typ01/cgi-bin/url/nph-sfc_obs_nc_api"
        self.auth_key = "ADb5ZbflRq22-WW35Satdw"
        self.nc_path = "../data/sfc_grid_latlon.nc"

    
        self._load_nc_coordinates()

    def _load_nc_coordinates(self):
        with nc.Dataset(self.nc_path) as ds:
            lats = ds.variables["lat"][:]
            lons = ds.variables["lon"][:]
        
        minx, miny, maxx, maxy = self.mapo_gdf.total_bounds
        bbox_mask = (lats >= miny - 0.01) & (lats <= maxy + 0.01) & (lons >= minx - 0.01) & (lons <= maxx + 0.01)
        y_indices, x_indices = np.where(bbox_mask)
        self.y_min, self.y_max = y_indices.min(), y_indices.max()
        self.x_min, self.x_max = x_indices.min(), x_indices.max()
        
        self.mapo_crop_lats = lats[self.y_min : self.y_max + 1, self.x_min : self.x_max + 1]
        self.mapo_crop_lons = lons[self.y_min : self.y_max + 1, self.x_min : self.x_max + 1]


    def _match_color(self, value, bounds, colors):
        """💡 단일 숫자 온도값(value)이 어느 bounds 구간에 속하는지 판별하여 매칭되는 HEX 색상을 반환"""
        if np.isnan(value):
            return "#FFFFFF"
        
        # 기온 데이터가 최솟값보다 작을 경우 첫 번째 색상 할당
        if value < bounds[0]:
            return colors[0]
            
        # 구간 탐색하면서 알맞은 색상 인덱스 매핑
        for i in range(len(bounds) - 1):
            if bounds[i] <= value < bounds[i+1]:
                return colors[i]
                
        # 기온 데이터가 정의된 최댓값보다 크거나 같으면 마지막 색상 할당
        return colors[-1]

    
    def generate_decal_image(self, tm, obs):
        """기존 원본의 고정밀 자동 실수 분할(Double 스케일)을 백프로 유지하고 색상만 변경"""
        params = {"obs": obs, "tm": tm, "disp": "B", "authKey": self.auth_key}
        response = requests.get(self.api_url, params=params, timeout=10)
        response.raise_for_status()
        
        binary_data = response.content
        nx, ny = struct.unpack("<HH", binary_data[:4])
        grid_values = struct.unpack(f"<{nx*ny}f", binary_data[4:4+(nx*ny*4)])
        
        grid_vis = np.array(grid_values)
        grid_vis[grid_vis == -999.00] = np.nan
        grid_2d = grid_vis.reshape((ny, nx))
        
        mapo_crop_grid = grid_2d[self.y_min : self.y_max + 1, self.x_min : self.x_max + 1]
        
        # 💡 [해결책] 원본처럼 자동 스케일링을 태우되, 색상 리스트만 주입합니다.
        # 원하셨던 15단계 열섬 전용 단색 컬러 명세 리스트를 줍니다.
        custom_colors = [
            '#FFFFE5', '#FFF7BC', '#FEE391', '#FEC44F', '#FB9A29', 
            '#EC7014', '#CC4C02', '#993404', '#A93226', '#CB4335',
            '#922B21', '#7B241C', '#641E16', '#4D1712', '#35100C'
        ]
        
        # 별도의 복잡한 bounds 공식(norm) 없이 비주얼라이저에 색상만 토스하여 
        # imshow가 내부 실수 정밀도 그대로 자동으로 이 15개 색상에 균등 배분하게 만듭니다.
        return DecalVisualizer.draw_kma_grid(
            mapo_crop_grid, self.mapo_crop_lats, self.mapo_crop_lons,
            self.mapo_dong_gdf, self.mapo_gdf, self.mapo_all_polygon,
            bounds=None, colors=custom_colors
        )



    def generate_decal_geojson(self, tm, obs):
        """GeoJSON 생성단도 원본의 자동 min/max 스케일을 모방하여 정밀 매핑"""
        params = {"obs": obs, "tm": tm, "disp": "B", "authKey": self.auth_key}
        response = requests.get(self.api_url, params=params, timeout=10)
        response.raise_for_status()

        binary_data = response.content
        nx, ny = struct.unpack("<HH", binary_data[:4])
        grid_values = struct.unpack(f"<{nx*ny}f", binary_data[4:4+(nx*ny*4)])

        grid_vis = np.array(grid_values)
        grid_vis[grid_vis == -999.00] = np.nan
        grid_2d = grid_vis.reshape((ny, nx))

        mapo_crop_grid = grid_2d[self.y_min : self.y_max + 1, self.x_min : self.x_max + 1]

        # 💡 원본 imshow의 스케일 메커니즘을 그대로 재현하기 위해 현재 격자의 실제 min, max를 구함
        valid_values = mapo_crop_grid[~np.isnan(mapo_crop_grid)]
        min_val = float(valid_values.min()) if len(valid_values) > 0 else 20.0
        max_val = float(valid_values.max()) if len(valid_values) > 0 else 35.0
        if min_val == max_val:
            max_val += 1.0

        custom_colors = [
            '#FFFFE5', '#FFF7BC', '#FEE391', '#FEC44F', '#FB9A29', 
            '#EC7014', '#CC4C02', '#993404', '#A93226', '#CB4335',
            '#922B21', '#7B241C', '#641E16', '#4D1712', '#35100C'
        ]
        
        # 현재 기온 폭(0.09도 수준)에 맞춰 정확하게 15개 구간으로 쪼갭니다.
        auto_bounds = np.linspace(min_val, max_val, len(custom_colors) + 1).tolist()

        features = []
        h, w = mapo_crop_grid.shape

        for y in range(h - 1):
            for x in range(w - 1):
                value = mapo_crop_grid[y][x]

                if np.isnan(value):
                    continue

                val_float = float(value)
                assigned_color = custom_colors[-1]
                for i in range(len(auto_bounds) - 1):
                    if auto_bounds[i] <= val_float < auto_bounds[i+1]:
                        assigned_color = custom_colors[i]
                        break

                polygon = [[
                    [float(self.mapo_crop_lons[y][x]), float(self.mapo_crop_lats[y][x])],
                    [float(self.mapo_crop_lons[y][x + 1]), float(self.mapo_crop_lats[y][x + 1])],
                    [float(self.mapo_crop_lons[y + 1][x + 1]), float(self.mapo_crop_lats[y + 1][x + 1])],
                    [float(self.mapo_crop_lons[y + 1][x]), float(self.mapo_crop_lats[y + 1][x])],
                    [float(self.mapo_crop_lons[y][x]), float(self.mapo_crop_lats[y][x])]
                ]]

                features.append({
                    "type": "Feature",
                    "geometry": {
                        "type": "Polygon",
                        "coordinates": polygon
                    },
                    "properties": {
                        "value": val_float,
                        "color_code": assigned_color
                    }
                })

        return {"type": "FeatureCollection", "features": features}










