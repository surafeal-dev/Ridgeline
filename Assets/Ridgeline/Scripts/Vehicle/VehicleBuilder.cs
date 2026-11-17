using Ridgeline.Art;
using UnityEngine;

namespace Ridgeline.Vehicle
{
    /// <summary>
    /// Builds a playable vehicle from a <see cref="VehicleConfig"/> at runtime:
    /// chassis + two wheels on WheelJoint2D + crash sensor. Uses the CC0 art from <see cref="ArtSet"/>
    /// when given, placeholder shapes otherwise. The physics shape comes from the config, not the art.
    /// </summary>
    public static class VehicleBuilder
    {
        // Placeholder palette (used only when no art is assigned).
        static readonly Color Body = new Color(0.20f, 0.45f, 0.42f);
        static readonly Color BodyDark = new Color(0.13f, 0.30f, 0.28f);
        static readonly Color Bar = new Color(0.15f, 0.15f, 0.17f);
        static readonly Color Tire = new Color(0.16f, 0.16f, 0.18f);
        static readonly Color Skin = new Color(0.96f, 0.78f, 0.62f);

        public static VehicleController Build(VehicleConfig config, Vector2 position, Material spriteMaterial, LayerMask groundMask, ArtSet art = null)
        {
            int vehicleLayer = Mathf.Max(0, LayerMask.NameToLayer("Vehicle"));

            var root = new GameObject(config.displayName);
            root.transform.position = Vector3.zero;

            // ---- Chassis ----
            var chassisGo = new GameObject("Chassis") { layer = vehicleLayer };
            chassisGo.transform.SetParent(root.transform, false);
            chassisGo.transform.position = position;
            var chassis = chassisGo.AddComponent<Rigidbody2D>();
            chassis.mass = config.chassisMass;
            chassis.linearDamping = config.chassisLinearDamping;
            chassis.angularDamping = config.chassisAngularDamping;
            chassis.interpolation = RigidbodyInterpolation2D.Interpolate;
            chassis.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var body = chassisGo.AddComponent<PolygonCollider2D>();
            body.points = config.bodyShape;
            chassis.centerOfMass = config.centerOfMass;

            bool hasArt = art != null && art.jeepBody != null;
            // Sorting: driver 8-9 (behind the see-through cab glass), body 10, wheels 12.
            if (hasArt)
            {
                var bodySr = Sprite(chassisGo.transform, "Body", art.jeepBody, Vector2.zero, 10, spriteMaterial);
                float scale = config.bodyArtWidth / art.jeepBody.bounds.size.x;
                bodySr.transform.localScale = new Vector3(scale, scale, 1f);
                bodySr.transform.localPosition = (Vector3)config.bodyArtOffset - art.jeepBody.bounds.center * scale;
            }
            else
            {
                PlaceholderBody(chassisGo.transform, spriteMaterial);
            }

            // Driver in the open cab: torso and arm sit behind the door (sorting 7-8 < body 10),
            // the oversized bobblehead sits in front of everything on a wobbling neck.
            var driverGo = new GameObject("Driver") { layer = vehicleLayer };
            driverGo.transform.SetParent(chassisGo.transform, false);
            driverGo.transform.localPosition = config.driverSeat;
            if (hasArt && art.driverBody != null)
            {
                var torso = Sprite(driverGo.transform, "Torso", art.driverBody, Vector2.zero, 8, spriteMaterial);
                float ts = config.driverTorsoHeight / art.driverBody.bounds.size.y;
                torso.transform.localScale = new Vector3(ts, ts, 1f);
                if (art.driverArm != null)
                {
                    // Arm reaching forward to the (hidden) steering wheel.
                    var arm = Sprite(driverGo.transform, "Arm", art.driverArm, new Vector2(0.17f, 0.04f), 9, spriteMaterial);
                    arm.transform.localScale = new Vector3(ts, ts, 1f);
                    arm.transform.localRotation = Quaternion.Euler(0f, 0f, 70f);
                }
            }

            var neck = new GameObject("Neck") { layer = vehicleLayer };
            neck.transform.SetParent(driverGo.transform, false);
            neck.transform.localPosition = new Vector3(0f, config.driverTorsoHeight * 0.5f, 0f);
            var wobble = neck.AddComponent<HeadWobble>();
            wobble.body = chassis;

            var headSprite = hasArt && art.driverHead != null ? art.driverHead : PlaceholderArt.Circle;
            var headSr = Sprite(neck.transform, "Head", headSprite, Vector2.zero, 14, spriteMaterial);
            float headScale = config.driverHeadHeight / headSprite.bounds.size.y;
            headSr.transform.localScale = new Vector3(headScale, headScale, 1f);
            // Head centre above the neck, with the chin overlapping the collar a little.
            headSr.transform.localPosition = new Vector3(0f, config.driverHeadHeight * 0.5f - 0.12f, 0f);
            if (!hasArt) headSr.color = Skin;

            // Crash sensor (cab roof / driver's head).
            var sensorGo = new GameObject("CrashSensor") { layer = vehicleLayer };
            sensorGo.transform.SetParent(chassisGo.transform, false);
            sensorGo.transform.localPosition = config.headOffset;
            var sensorCol = sensorGo.AddComponent<CircleCollider2D>();
            sensorCol.isTrigger = true;
            sensorCol.radius = config.headRadius;
            var head = sensorGo.AddComponent<DriverHead>();
            head.face = headSr;
            head.hurtFace = hasArt ? art.driverHeadHurt : null;

            // ---- Wheels ----
            var rear = BuildWheel(root.transform, "RearWheel", chassisGo.transform.TransformPoint(config.rearWheelAnchor), config, vehicleLayer, spriteMaterial, art);
            var front = BuildWheel(root.transform, "FrontWheel", chassisGo.transform.TransformPoint(config.frontWheelAnchor), config, vehicleLayer, spriteMaterial, art);

            var rearJoint = AddWheelJoint(chassisGo, rear, config.rearWheelAnchor);
            var frontJoint = AddWheelJoint(chassisGo, front, config.frontWheelAnchor);

            // ---- Controller ----
            var vc = root.AddComponent<VehicleController>();
            vc.config = config;
            vc.chassis = chassis;
            vc.rearWheel = rear;
            vc.frontWheel = front;
            vc.rearJoint = rearJoint;
            vc.frontJoint = frontJoint;
            vc.rearWheelCollider = rear.GetComponent<CircleCollider2D>();
            vc.frontWheelCollider = front.GetComponent<CircleCollider2D>();
            vc.groundMask = groundMask;
            head.vehicle = vc;
            vc.ApplyUpgrades(0, 0, 0);
            return vc;
        }

        static void PlaceholderBody(Transform t, Material m)
        {
            Rect(t, "Body", new Vector2(0f, -0.1f), new Vector2(3.3f, 0.7f), Body, 10, m);
            Rect(t, "Cab", new Vector2(0.15f, 0.35f), new Vector2(0.6f, 0.5f), BodyDark, 10, m);
            Rect(t, "Bumper", new Vector2(1.68f, -0.25f), new Vector2(0.18f, 0.35f), Bar, 11, m);
        }

        static Rigidbody2D BuildWheel(Transform parent, string name, Vector3 worldPos, VehicleConfig c, int layer, Material mat, ArtSet art)
        {
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.mass = c.wheelMass;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.angularDamping = 0.05f;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = c.wheelRadius;

            // The visual is a scaled child, so the collider radius stays in world units.
            if (art != null && art.jeepTire != null)
            {
                var sr = Sprite(go.transform, "Tire", art.jeepTire, Vector2.zero, 12, mat);
                float s = c.wheelRadius * 2f / art.jeepTire.bounds.size.x;
                sr.transform.localScale = new Vector3(s, s, 1f);
            }
            else
            {
                var vis = Circle(go.transform, "Tire", Vector2.zero, c.wheelRadius * 2f, new Color(0.85f, 0.85f, 0.85f), 12, mat);
                vis.sprite = PlaceholderArt.Ring;
                Rect(go.transform, "Spoke", Vector2.zero, new Vector2(c.wheelRadius * 1.1f, 0.08f), Tire, 13, mat);
            }
            return rb;
        }

        static WheelJoint2D AddWheelJoint(GameObject chassis, Rigidbody2D wheel, Vector2 anchor)
        {
            var j = chassis.AddComponent<WheelJoint2D>();
            j.connectedBody = wheel;
            j.autoConfigureConnectedAnchor = false;
            j.anchor = anchor;
            j.connectedAnchor = Vector2.zero;
            j.enableCollision = false;
            j.useMotor = false;
            return j;
        }

        static SpriteRenderer Sprite(Transform parent, string name, Sprite sprite, Vector2 localPos, int order, Material mat)
        {
            var go = new GameObject(name);
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (mat) sr.sharedMaterial = mat;
            return sr;
        }

        static SpriteRenderer Rect(Transform parent, string name, Vector2 localPos, Vector2 size, Color color, int order, Material mat)
        {
            var sr = Sprite(parent, name, PlaceholderArt.Square, localPos, order, mat);
            sr.transform.localScale = new Vector3(size.x, size.y, 1f);
            sr.color = color;
            return sr;
        }

        static SpriteRenderer Circle(Transform parent, string name, Vector2 localPos, float diameter, Color color, int order, Material mat)
        {
            var sr = Sprite(parent, name, PlaceholderArt.Circle, localPos, order, mat);
            sr.transform.localScale = Vector3.one * diameter;
            sr.color = color;
            return sr;
        }
    }
}
