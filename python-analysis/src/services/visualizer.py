import io
import numpy as np
import matplotlib
matplotlib.use('Agg')  # GUI 화면이 없는 서버 환경에서 팝업 에러가 발생하는 것을 방지
import matplotlib.pyplot as plt
import matplotlib.patches as patches
import matplotlib.colors as mcolors  # 💡 단색 분할(BoundaryNorm) 매핑을 위해 추가
from shapely.geometry import Polygon, MultiPolygon

class DecalVisualizer:
    @staticmethod
    def _get_patch(poly):
        """Shapely Geometry 객체를 Matplotlib Patch 객체로 변환"""
        if isinstance(poly, Polygon):
            return patches.Polygon(np.array(poly.exterior.coords), closed=True)
        elif isinstance(poly, MultiPolygon):
            return [patches.Polygon(np.array(p.exterior.coords), closed=True) for p in poly.geoms]

    @staticmethod
    def _apply_common_decal_style(fig, ax, lon_min, lon_max, lat_min, lat_max, dong_gdf, mapo_gdf):
        """배경 투명화, 축 삭제, 경계선 드로잉 및 메모리 버퍼 저장을 일괄 처리하는 공통 메서드"""
        # 그림 자체의 배경과 축의 배경을 모두 투명('none')으로 설정
        fig.patch.set_facecolor('none')
        ax.set_facecolor('none')
        
        # 1. 내부 법정동 경계선 그리기 (진한 실선)
        dong_gdf.plot(ax=ax, facecolor="none", edgecolor="black", linewidth=1.2, linestyle="-")
        
        # 2. 마포구 전체 외곽 테두리 두껍게 강조
        mapo_gdf.plot(ax=ax, facecolor="none", edgecolor="black", linewidth=3.0)
        
        # 3. 유니티 데칼 전용 UI 오프 설정 (축, 눈금 글자 모두 제거)
        ax.axis('off')
        ax.set_xlim(lon_min, lon_max)
        ax.set_ylim(lat_min, lat_max)

        # 4. [핵심 데이터 가공] 디스크 파일이 아닌 메모리 버퍼(BytesIO)에 PNG 바이너리 쓰기
        img_buffer = io.BytesIO()
        plt.savefig(
            img_buffer, 
            dpi=512,            # 유니티 해상도 요구치에 맞춰 512, 1024 등으로 변경 가능
            transparent=True,   # 배경 투명화 플래그
            bbox_inches='tight', 
            pad_inches=0
        )
        img_buffer.seek(0)      # 데이터 스트림의 읽기 포인터를 처음 위치로 초기화
        
        plt.close(fig)          # 서버 메모리 누수 방지를 위한 객체 해제
        return img_buffer

    @staticmethod
    def draw_kma_grid(crop_grid, crop_lats, crop_lons, dong_gdf, mapo_gdf, all_polygon, bounds=None, colors=None):
        """1. 기상청 연속 격자 데이터 드로잉 및 쿠키커터 정밀 클리핑"""
        fig, ax = plt.subplots(figsize=(10, 10), subplot_kw={'aspect': 'equal'})
        lon_min, lon_max = crop_lons.min(), crop_lons.max()
        lat_min, lat_max = crop_lats.min(), crop_lats.max()

        # 💡 [핵심 추가] 서비스 레이어에서 주입한 bounds와 colors를 기반으로 컬러맵 빌드
        if bounds is not None and colors is not None:
            custom_cmap = mcolors.ListedColormap(colors)
            norm = mcolors.BoundaryNorm(bounds, custom_cmap.N)
        else:
            custom_cmap = "jet"  # 예외 대응 백업용 기본값
            norm = None

        # 기상 격자 배경 이미지 출력
        im = ax.imshow(
            crop_grid, 
            cmap=custom_cmap,  # 💡 주입받은 커스텀 단색 조합 적용
            norm=norm,         # 💡 특정 온도 구간에서 딱 끊기도록 세팅
            origin="lower", 
            extent=[lon_min, lon_max, lat_min, lat_max]
        )

        # 쿠키커터 클리핑 패치 적용 (마포구 외곽선 내부로만 이미지 제한)
        patch = DecalVisualizer._get_patch(all_polygon)
        if isinstance(patch, list):
            for p in patch:
                ax.add_patch(p)
                p.set_visible(False)
                im.set_clip_path(p)
        else:
            ax.add_patch(patch)
            patch.set_visible(False)
            im.set_clip_path(patch)

        # 공통 스타일러를 거쳐 메모리 스트림 반환
        return DecalVisualizer._apply_common_decal_style(
            fig, ax, lon_min, lon_max, lat_min, lat_max, dong_gdf, mapo_gdf
        )

    @staticmethod
    def draw_sdot_points(lons, lats, values, dong_gdf, mapo_gdf, all_polygon):
        """2. S-DoT 산점도 포인트 데이터 드로잉 (센서별 반경 표현 방식)"""
        fig, ax = plt.subplots(figsize=(10, 10), subplot_kw={'aspect': 'equal'})
        
        # 마포구 경계 기반 좌표 한계치 스케일 획득
        minx, miny, maxx, maxy = mapo_gdf.total_bounds
        
        # 개별 센서 위치에 데이터(온도 등) 크기에 따른 포인트 열지도 출력
        # s=200은 점의 크기입니다. 유니티 맵 스케일에 맞춰 조절하세요.
        ax.scatter(
            lons, lats, 
            c=values, 
            cmap="jet", 
            s=200, 
            edgecolors='none', 
            alpha=0.9
        )
        
        # S-DoT은 포인트 필터링을 통해 이미 마포구 내부에만 점이 찍히므로 바로 스타일러로 이관
        return DecalVisualizer._apply_common_decal_style(
            fig, ax, minx, maxx, miny, maxy, dong_gdf, mapo_gdf
        )
