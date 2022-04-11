using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    public enum CameraMode{
        TopDown=0,
        ThirdPerson = 10,
        ThirdPersonShooter = 20, //Not working on mobile, require mouse
    }

    /// <summary>
    /// Main camera script
    /// </summary>

    public class TheCamera : MonoBehaviour
    {
        public CameraMode mode;

        [Header("Move/Zoom")]
        public float move_speed = 2f;
        public float rotate_speed = 90f;
        public float zoom_speed = 0.5f;
        public float zoom_speed_touch = 1f;
        public float zoom_in_max = 0.5f;
        public float zoom_out_max = 1f;

        [Header("TPS Only")]
        public float freelook_speed_x = 0f;
        public float freelook_speed_y = 0f;
        public bool toggle_freelook = false; //If set to true, toggle on click, otherwise hold.

        [Header("Target")]
        public GameObject follow_target;
        public Vector3 follow_offset;
        public Vector3 lookat_offset;

        private Vector3 current_vel;
        private Vector3 rotated_offset;
        private Vector3 current_offset;
        private float current_rotate = 0f;
        private float current_zoom = 0f;
        private Transform target_transform;

        private Camera cam;

        private Vector3 shake_vector = Vector3.zero;
        private float shake_timer = 0f;
        private float shake_intensity = 1f;

        private static TheCamera _instance;

        void Awake()
        {
            _instance = this;
            cam = GetComponent<Camera>();
            rotated_offset = follow_offset;
            current_offset = follow_offset;

            GameObject cam_target = new GameObject("CameraTarget");
            target_transform = cam_target.transform;
            target_transform.position = transform.position;
            target_transform.rotation = transform.rotation;
        }

        private void Start()
        {
            if (follow_target == null && PlayerCharacter.Get())
            {
                follow_target = PlayerCharacter.Get().gameObject;
            }

            if (mode == CameraMode.ThirdPersonShooter)
            {
                Cursor.lockState =  CursorLockMode.Locked;
                Cursor.visible = false;
            }

            PlayerControlsMouse mouse = PlayerControlsMouse.Get();
            //mouse.onClick += (Vector3 vect) => { if(!IsLocked()) SetLockMode(true); };
            mouse.onRightClick += (Vector3 vect) => { ToggleLock(); };
        }

        void LateUpdate()
        {
            PlayerControls controls = PlayerControls.Get();
            PlayerControlsMouse mouse = PlayerControlsMouse.Get();

            //Rotate
            current_rotate = controls.GetRotateCam();
            if (mode == CameraMode.TopDown)
                current_rotate = -current_rotate; //Reverse rotate

            //Zoom 
            current_zoom += mouse.GetMouseScroll() * zoom_speed; //Mouse scroll zoom
            current_zoom += mouse.GetTouchZoom() * zoom_speed_touch; //Mobile 2 finger zoom
            current_zoom = Mathf.Clamp(current_zoom, -zoom_out_max, zoom_in_max);

            if (!toggle_freelook && mode == CameraMode.ThirdPersonShooter)
                SetLockMode(mouse.IsMouseHoldRight());
            
            if (!IsLocked())
                UpdateNormal();

            if (IsLocked())
                UpdateShooter();

            //Untoggle if on top of UI
            if (toggle_freelook && IsLocked() && TheUI.Get() && TheUI.Get().IsBlockingPanelOpened())
                ToggleLock();

            //Shake FX
            if (shake_timer > 0f)
            {
                shake_timer -= Time.deltaTime;
                shake_vector = new Vector3(Mathf.Cos(shake_timer * Mathf.PI * 8f) * 0.02f, Mathf.Sin(shake_timer * Mathf.PI * 7f) * 0.02f, 0f);
                transform.position += shake_vector * shake_intensity;
            }
        }

        private void UpdateNormal()
        {
            rotated_offset = Quaternion.Euler(0, rotate_speed * current_rotate * Time.deltaTime, 0) * rotated_offset;
            current_offset = rotated_offset - rotated_offset * current_zoom;

            target_transform.RotateAround(follow_target.transform.position, Vector3.up, rotate_speed * current_rotate * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, target_transform.rotation, move_speed * Time.deltaTime);

            Vector3 target_pos = follow_target.transform.position + current_offset;
            target_transform.position = target_pos;
            transform.position = Vector3.SmoothDamp(transform.position, target_pos, ref current_vel, 1f / move_speed);
        }

        private void UpdateShooter()
        {
            PlayerControlsMouse mouse = PlayerControlsMouse.Get();
            Vector2 mouse_delta = mouse.GetMouseDelta();

            Quaternion target_backup = target_transform.transform.rotation;
            Vector3 rotate_backup = rotated_offset;

            rotated_offset = Quaternion.AngleAxis(freelook_speed_y * -mouse_delta.y * 0.5f * Time.deltaTime, target_transform.right) * rotated_offset;
            rotated_offset = Quaternion.Euler(0f, freelook_speed_x * mouse_delta.x * Time.deltaTime, 0) * rotated_offset;
            current_offset = rotated_offset - rotated_offset * current_zoom;

            target_transform.RotateAround(follow_target.transform.position, target_transform.right, freelook_speed_y * -mouse_delta.y * Time.deltaTime);
            target_transform.RotateAround(follow_target.transform.position, Vector3.up, freelook_speed_x * mouse_delta.x * Time.deltaTime);

            //Lock to not rotate too much
            if (target_transform.transform.up.y < 0.2f)
            {
                target_transform.transform.rotation = target_backup;
                rotated_offset = rotate_backup;
            }

            Vector3 target_pos = follow_target.transform.position + current_offset;
            target_transform.position = target_pos;
            transform.position = Vector3.Lerp(transform.position, target_pos, move_speed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, target_transform.rotation, move_speed * Time.deltaTime);
        }

        public void SetLockMode(bool locked)
        {
            if (mode == CameraMode.ThirdPersonShooter)
            {
                Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !locked;
            }
        }

        public void ToggleLock()
        {
            if (toggle_freelook)
            {
                SetLockMode(Cursor.lockState != CursorLockMode.Locked);
            }
        }

        public void MoveToTarget(Vector3 target)
        {
            transform.position = target + current_offset;
        }

        public void Shake(float intensity = 2f, float duration = 0.5f)
        {
            shake_intensity = intensity;
            shake_timer = duration;
        }

        public Vector3 GetTargetPos()
        {
            return transform.position - current_offset;
        }

        //Use as center for optimization
        public Vector3 GetTargetPosOffsetFace(float dist)
        {
            return transform.position - current_offset + GetFacingFront() * dist;
        }

        public Quaternion GetRotation()
        {
            return Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        }

        public Vector3 GetFacingFront()
        {
            Vector3 dir = transform.forward;
            dir.y = 0f;
            return dir.normalized;
        }

        public Vector3 GetFacingRight()
        {
            Vector3 dir = transform.right;
            dir.y = 0f;
            return dir.normalized;
        }

        public Quaternion GetFacingRotation()
        {
            Vector3 facing = GetFacingFront();
            return Quaternion.LookRotation(facing.normalized, Vector3.up);
        }

        public bool IsLocked()
        {
            return Cursor.lockState == CursorLockMode.Locked;
        }

        public Camera GetCam()
        {
            return cam;
        }

        public static Camera GetCamera()
        {
            Camera camera = _instance != null ? _instance.GetCam() : Camera.main;
            return camera;
        }

        public static TheCamera Get()
        {
            return _instance;
        }
    }

}