using UnityEngine;

namespace DodgeRunner
{
    // 床セグメントをゲーム速度で後方に流し、カメラ後方へ抜けたら列の先頭へ戻す（無限地面）。
    public class TrackScroller : MonoBehaviour
    {
        [SerializeField] float segmentLength = 20f;
        [SerializeField] float recycleZ = -30f;

        Transform[] segments;

        void Start()
        {
            segments = new Transform[transform.childCount];
            for (var i = 0; i < segments.Length; i++) segments[i] = transform.GetChild(i);
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            var dz = gm.Speed * Time.deltaTime;
            var maxZ = float.MinValue;
            foreach (var s in segments) maxZ = Mathf.Max(maxZ, s.position.z);

            foreach (var s in segments)
            {
                s.position += Vector3.back * dz;
                var passedCamera = s.position.z < recycleZ;
                if (!passedCamera) continue;
                s.position = new Vector3(s.position.x, s.position.y, maxZ + segmentLength);
                maxZ = s.position.z;
            }
        }
    }
}
