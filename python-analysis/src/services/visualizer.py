import io
import numpy as np
import matplotlib
matplotlib.use('Agg')  # GUI 화면이 없는 서버 환경에서 팝업 에러가 발생하는 것을 방지
import matplotlib.pyplot as plt
import matplotlib.patches as patches
import matplotlib.colors as mcolors 
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
        ax.set_xlim(lon_min, lon_max)
        ax.set_ylim(lat_min, lat_max)
        ax.axis('off')

        ax.set_position([0,0,1,1])

        fig.subplots_adjust(
            left=0,
            right=1,
            top=1,
            bottom=0
        )

        img_buffer = io.BytesIO()
        print("FIG SIZE:", fig.get_size_inches())
        print("DPI:", fig.dpi)

        fig.savefig(
            img_buffer,
            dpi=512,
            transparent=True,
            bbox_inches=None,
            pad_inches=0
        )

        img_buffer.seek(0)

        plt.close(fig)

        return img_buffer

    @staticmethod
    def draw_kma_grid(crop_grid, crop_lats, crop_lons, dong_gdf, mapo_gdf, all_polygon, bounds=None, colors=None):
        """1. 기상청 연속 격자 데이터 드로잉 및 쿠키커터 정밀 클리핑"""
        
        lon_step = abs(crop_lons[0][1] - crop_lons[0][0])
        lat_step = abs(crop_lats[1][0] - crop_lats[0][0])

        lon_min = crop_lons.min() - lon_step * 0.5
        lon_max = crop_lons.max() + lon_step * 0.5

        lat_min = crop_lats.min() - lat_step * 0.5
        lat_max = crop_lats.max() + lat_step * 0.5

        width = lon_max - lon_min
        height = lat_max - lat_min
        aspect = width / height


        fig = plt.figure(
            figsize=(10 * aspect, 10),
            dpi=512
        )
        ax = fig.add_axes([0,0,1,1])
        

        # 💡 [핵심 추가] 서비스 레이어에서 주입한 bounds와 colors를 기반으로 컬러맵 빌드
        if bounds is not None and colors is not None:
            custom_cmap = mcolors.ListedColormap(colors)
            norm = mcolors.BoundaryNorm(bounds, custom_cmap.N)
        else:
            custom_cmap = "jet"  # 예외 대응 백업용 기본값
            norm = None

        # 기상 격자 배경 이미지 출력
        im = ax.imshow(
            crop_grid[:-1, :-1],
            cmap=custom_cmap,
            norm=norm,
            origin="lower",
            extent=[
                lon_min,
                lon_max,
                lat_min,
                lat_max
            ],
            interpolation="nearest"
        )

        ax.set_xlim(lon_min, lon_max)
        ax.set_ylim(lat_min, lat_max)

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


