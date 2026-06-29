import json
import base64
import io
from flask import Flask, request, send_file, jsonify, Response
from services import WeatherServiceFactory

app = Flask(__name__)


# =========================
# 1. 기상청 전용 API 
# =========================
@app.route('/api/weather/mapo-decal.geojson', methods=['GET'])
def get_mapo_geojson():

    source = request.args.get( 'source', 'kma')
    tm=request.args.get('tm')
    obs=request.args.get( 'obs', 'ta' )
    service = WeatherServiceFactory.get_service(source)

    data = service.generate_decal_geojson( tm, obs )

    return jsonify(data)





# =========================
# 2. PNG 전용 API (Cesium용)
# =========================
@app.route('/api/weather/mapo-decal.png', methods=['GET'])
def get_mapo_png():
    source = request.args.get('source', 'kma')
    tm = request.args.get('tm')
    obs = request.args.get('obs', 'ta')

    if not tm:
        return jsonify({"error": "Missing tm parameter"}), 400

    try:
        service = WeatherServiceFactory.get_service(source)
        image_stream = service.generate_decal_image(tm, obs)

        image_stream.seek(0)
        return send_file(image_stream, mimetype='image/png')

    except Exception as e:
        return jsonify({"error": str(e)}), 500


# =========================
# 2. bounds 전용 API -> 데칼작업에 고정해뒀음
# =========================

# @app.route('/api/weather/mapo-decal.json', methods=['GET'])
# def get_mapo_bounds():
#     return jsonify({
#         "bounds": {
#             "minLon": 126.81,
#             "maxLon": 126.95,
#             "minLat": 37.52,
#             "maxLat": 37.60
#         }
#     })

# =========================
# 3. 센서 데이터
# =========================

# @app.route('/api/sdot/mapo')
# def get_mapo_sdot():

#     date = request.args.get("date")

#     service = WeatherServiceFactory.get_service("sdot")
#     df = service.get_latest_mapo_data(date)

#     df["TIME"] = df["TIME"].astype(str)

#     return jsonify(df.to_dict(orient="records"))


if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000)


# http://127.0.0.1:5000/api/weather/mapo-decal.geojson?tm=202408112300
# png 테스트   http://127.0.0.1:5000/api/weather/mapo-decal.png?tm=202408112300
# 좌표         http://127.0.0.1:5000/api/weather/mapo-decal.json
# 센서 데이터   http://127.0.0.1:5000/api/sdot/mapo?date=2026-06-21

