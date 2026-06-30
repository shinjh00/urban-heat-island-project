from .kma_service import KmaWeatherService

class WeatherServiceFactory:
    """[Factory] 클라이언트 요청 소스 문자열에 따라 알맞은 서비스를 동적 반환"""
    _services = {
        "kma": KmaWeatherService()
    }

    @staticmethod
    def get_service(source_name: str):
        service = WeatherServiceFactory._services.get(source_name.lower())
        if not service:
            raise ValueError(f"Unsupported data source: '{source_name}'. Choose 'kma' or 'sdot'.")
        return service
