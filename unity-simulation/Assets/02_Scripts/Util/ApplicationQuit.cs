using UnityEngine;

public class ApplicationQuit : MonoBehaviour
{

    public void QuitGame()
    {
        Debug.Log("[ApplicationQuit] 프로그램을 안전하게 종료합니다.");

        // 전처리기를 사용해 에디터와 실제 빌드 환경의 코드를 완벽히 분리합니다.
        #if UNITY_EDITOR
        // using UnityEditor;를 맨 위에 쓰지 않고, 여기에 직접 전체 경로를 적어줍니다.
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        // 실제 빌드된 프로그램(.exe)이 꺼질 때 동작
        Application.Quit();
        #endif
    }

    private void OnApplicationQuit()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        System.GC.Collect();
    }

}
