using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace SurvivalEngine
{

    /// <summary>
    /// Mouse/Touch controls manager
    /// </summary>

    public class PlayerControlsMouse : MonoBehaviour
    {
        public LayerMask selectable_layer = ~0;
        public LayerMask floor_layer = (1 << 9); //Put to none to always return 0 as floor height
        public float mobile_joystick_sensitivity = 0.08f; //In percentage before reaching full speed
        public float mobile_joystick_threshold = 0.02f; //In percentage relative to Screen.height

        public UnityAction<Vector3> onClick; //Always triggered on left click
        public UnityAction<Vector3> onRightClick; //Always triggered on right click
        public UnityAction<Vector3> onLongClick; //When holding the left click down for 1+ sec
        public UnityAction<Vector3> onHold; //While holding the left click down
        public UnityAction<Vector3> onClickFloor; //When click on floor
        public UnityAction<Selectable, Vector3> onClickObject; //When click on object

        private bool using_mouse = false;
        private float mouse_scroll = 0f;
        private Vector2 mouse_delta = Vector2.zero;
        private bool mouse_hold_left = false;
        private bool mouse_hold_right = false;

        private float using_timer = 0f;
        private float hold_timer = 0f;
        private bool is_holding = false;
        private bool can_long_click = false;
        private Vector3 hold_start;
        private Vector3 last_pos;
        private Vector3 floor_pos; //World position the floor pointing at

        private bool joystick_active = false;
        private Vector3 joystick_pos;
        private Vector3 joystick_dir;

        private float zoom_value = 0f;
        private bool is_zoom_mode = false;
        private float prev_distance = 0f;

        private HashSet<GameObject> raycast_list = new HashSet<GameObject>();

        private static PlayerControlsMouse _instance;

        void Awake()
        {
            _instance = this;
            last_pos = Input.mousePosition;
        }

        void Update()
        {
            //If not mobile, always check for raycast (on mobile it does it only after a click)
            if (!TheGame.IsMobile())
            {
                RaycastSelectables();
                RaycastFloorPos();
            }

            //Mouse click
            if (Input.GetMouseButtonDown(0))
            {
                hold_start = Input.mousePosition;
                is_holding = true;
                can_long_click = true;
                hold_timer = 0f;
                OnMouseClick();
            }

            if (Input.GetMouseButtonDown(1))
            {
                OnRightMouseClick();
            }

            //Mouse scroll
            mouse_scroll = Input.mouseScrollDelta.y;

            //Mouse delta
            mouse_delta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

            //Check for mouse usage
            Vector3 diff = (Input.mousePosition - last_pos);
            float dist = diff.magnitude;
            if (dist > 0.01f)
            {
                using_mouse = true;
                using_timer = 1f;
                last_pos = Input.mousePosition;
            }

            mouse_hold_left = Input.GetMouseButton(0) && !IsMouseOverUI();
            mouse_hold_right = Input.GetMouseButton(1) && !IsMouseOverUI();
            if (mouse_hold_left || mouse_hold_right)
                using_timer = 1f;

            //Is using mouse? (vs keyboard)
            using_timer -= Time.deltaTime;
            using_mouse = using_timer > 0f;

            //Long mouse click
            float dist_hold = (Input.mousePosition - hold_start).magnitude;
            is_holding = is_holding && mouse_hold_left;
            can_long_click = can_long_click && mouse_hold_left && dist_hold < 5f;

            if (is_holding)
            {
                hold_timer += Time.deltaTime;
                if (can_long_click && hold_timer > 0.5f)
                {
                    can_long_click = false;
                    hold_timer = 0f;
                    OnLongMouseClick();
                }
                else if (!can_long_click && hold_timer > 0.2f)
                {
                    OnMouseHold();
                }
            }

            //Mobile joystick and zoom
            if (TheGame.IsMobile())
            {
                if (Input.GetMouseButtonDown(0))
                {
                    joystick_pos = Input.mousePosition;
                    joystick_dir = Vector2.zero;
                    joystick_active = false;
                }

                if (!Input.GetMouseButton(0))
                {
                    joystick_active = false;
                    joystick_dir = Vector2.zero;
                }

                if (Input.GetMouseButton(0))
                {
                    Vector3 distance = Input.mousePosition - joystick_pos;
                    distance.z = 0f;
                    distance = distance / (float)Screen.height; //Scaled dist (square)
                    if (distance.magnitude > mobile_joystick_threshold)
                        joystick_active = true;

                    joystick_dir = distance / mobile_joystick_sensitivity;
                    joystick_dir = joystick_dir.normalized * Mathf.Min(joystick_dir.magnitude, 1f);
                    if (distance.magnitude < mobile_joystick_threshold)
                        joystick_dir = Vector2.zero;
                }

                zoom_value = 0f;
                if (Input.touchCount == 2)
                {
                    Vector3 pos1 = Input.GetTouch(0).position;
                    Vector3 pos2 = Input.GetTouch(1).position;
                    float distance = Vector2.Distance(pos1, pos2);
                    if (is_zoom_mode)
                    {
                        zoom_value = (distance - prev_distance) / (float)Screen.height;
                    }
                    prev_distance = distance;
                    is_zoom_mode = true; //Wait one frame to make sure distance has been calculated once
                    joystick_active = false; //No joystick while zooming
                }
                else
                {
                    is_zoom_mode = false;
                }
            }
        }

        public void RaycastSelectables()
        {
            raycast_list.Clear();
            RaycastHit[] hits = Physics.RaycastAll(GetMouseCameraRay(), 99f, selectable_layer.value);
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider != null)
                {
                    Selectable select = hit.collider.GetComponentInParent<Selectable>();
                    if (select != null)
                        raycast_list.Add(select.gameObject);
                }
            }
        }

        public void RaycastFloorPos()
        {
            Ray ray = GetMouseCameraRay();
            RaycastHit hit;
            bool success = Physics.Raycast(ray, out hit, 100f, floor_layer.value);
            if (success)
            {
                floor_pos = ray.GetPoint(hit.distance);
            }
            else
            {
                Plane plane = new Plane(Vector3.up, 0f);
                float dist;
                bool phit = plane.Raycast(ray, out dist);
                if (phit)
                {
                    floor_pos = ray.GetPoint(dist);
                }
            }

            //Debug.DrawLine(TheCamera.GetCamera().transform.position, floor_pos);
        }

        private void MobileCheckRaycast()
        {
            //If mobile, only check for raycast on click (on desktop it does it every frame in Update)
            if (TheGame.IsMobile())
            {
                RaycastSelectables();
                RaycastFloorPos();
            }
        }

        private void OnMouseClick()
        {
            if (IsMouseOverUI())
                return;

            MobileCheckRaycast();

            if (CraftBar.Get())
                CraftBar.Get().CancelSubSelection();

            Selectable hovered = GetNearestRaycastList(floor_pos);
            if (hovered != null)
            {
                if (onClick != null)
                    onClick.Invoke(hovered.transform.position);
                if (onClickObject != null)
                    onClickObject.Invoke(hovered, floor_pos);
            }
            else
            {
                if (onClick != null)
                    onClick.Invoke(floor_pos);
                if (onClickFloor != null)
                    onClickFloor.Invoke(floor_pos);
            }
        }

        private void OnRightMouseClick()
        {
            if (IsMouseOverUI())
                return;

            MobileCheckRaycast();

            if (onRightClick != null)
                onRightClick.Invoke(floor_pos);
        }

        //When holding for 1+ sec
        private void OnLongMouseClick()
        {
            if (IsMouseOverUI())
                return;

            MobileCheckRaycast();

            if (onLongClick != null)
                onLongClick.Invoke(floor_pos);
        }

        private void OnMouseHold()
        {
            if (IsMouseOverUI())
                return;

            MobileCheckRaycast();

            if (onHold != null)
                onHold.Invoke(floor_pos);
        }

        public Selectable GetNearestRaycastList(Vector3 pos)
        {
            Selectable nearest = null;
            float min_dist = 99f;
            foreach (GameObject obj in raycast_list)
            {
                Selectable select = obj.GetComponent<Selectable>();
                if (select != null && select.IsActive())
                {
                    float dist = (select.transform.position - pos).magnitude;
                    if (dist < min_dist)
                    {
                        min_dist = dist;
                        nearest = select;
                    }
                }
            }
            return nearest;
        }

        public int GetInventorySelectedSlotIndex()
        {
            if (InventoryBar.Get())
                return InventoryBar.Get().GetSelectedSlotIndex();
            return -1;
        }

        public int GetEquippedSelectedSlotIndex()
        {
            if (EquipBar.Get())
                return EquipBar.Get().GetSelectedSlotIndex();
            return -1;
        }

        public Vector2 GetScreenPos()
        {
            //In percentage
            Vector3 mpos = Input.mousePosition;
            return new Vector2(mpos.x / (float)Screen.width, mpos.y / (float)Screen.height);
        }

        public Vector3 GetPointingPos()
        {
            return floor_pos;
        }

        public bool IsInRaycast(GameObject obj)
        {
            return raycast_list.Contains(obj);
        }

        public bool IsUsingMouse()
        {
            return using_mouse;
        }

        public bool IsMouseHold()
        {
            return mouse_hold_left;
        }

        public bool IsMouseHoldRight()
        {
            return mouse_hold_right;
        }

        public float GetMouseScroll()
        {
            return mouse_scroll;
        }

        public Vector2 GetMouseDelta()
        {
            return mouse_delta;
        }

        public float GetTouchZoom()
        {
            return zoom_value;
        }

        public bool IsJoystickActive()
        {
            return joystick_active;
        }

        //In screen space
        public Vector2 GetJoystickPos()
        {
            return joystick_pos;
        }

        //Vector from (-1f,-1f) to (1f,1f)
        public Vector2 GetJoystickDir()
        {
            return joystick_dir;
        }

        //Clamped to screen mouse pos
        private Vector3 GetClampMousePos()
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.x = Mathf.Clamp(mousePos.x, 0f, Screen.width);
            mousePos.y = Mathf.Clamp(mousePos.y, 0f, Screen.height);
            return mousePos;
        }

        //Get ray from camera to mouse
        private Ray GetMouseCameraRay()
        {
            return TheCamera.GetCamera().ScreenPointToRay(GetClampMousePos());
        }

        //Check if mouse is on top of any UI element
        public bool IsMouseOverUI()
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            return results.Count > 0;
        }

        public static PlayerControlsMouse Get()
        {
            return _instance;
        }
    }

}
