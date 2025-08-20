using UnityEngine;

namespace BananaRun.Runner
{
    public class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 4f, -7f);
        public float followLerp = 10f;
        public float lookLerp = 12f;

        private void LateUpdate()
        {
            if (target == null) return;

            // 플레이어가 제자리에 있으므로 카메라도 고정 위치
            Vector3 desiredPos = target.position + offset;
            
            // 좌우 이동만 부드럽게 따라가고, Y/Z는 고정 오프셋 유지
            Vector3 currentPos = transform.position;
            currentPos.x = Mathf.Lerp(currentPos.x, desiredPos.x, 1f - Mathf.Exp(-followLerp * Time.deltaTime));
            currentPos.y = target.position.y + offset.y;
            currentPos.z = target.position.z + offset.z;
            transform.position = currentPos;

            Vector3 lookDir = (target.position - transform.position).normalized;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion desiredRot = Quaternion.LookRotation(lookDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, 1f - Mathf.Exp(-lookLerp * Time.deltaTime));
            }
        }
    }
}


