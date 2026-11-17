using System.Collections;
using NUnit.Framework;
using Ridgeline.Game;
using Ridgeline.InputControl;
using Ridgeline.Platform;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Ridgeline.Tests
{
    /// <summary>
    /// Loads the real Main scene and drives the Jeep with the pedals, the same way a player does.
    /// </summary>
    public class DrivingTests
    {
        GameManager _gm;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            PedalInput.ReleaseAll();
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            _gm = Object.FindAnyObjectByType<GameManager>();
            Assert.IsNotNull(_gm, "GameManager missing from Main scene");

            float t = 10f;
            while (_gm.State != GameState.Garage && t > 0f) { t -= Time.unscaledDeltaTime; yield return null; }
            Assert.AreEqual(GameState.Garage, _gm.State, "game never reached the garage");
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            PedalInput.ReleaseAll();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Boot_SendsFirstFrameThenGameReady()
        {
            var bridge = PlayablesBridge.Instance;
            Assert.IsNotNull(bridge);
            yield return null;
            Assert.IsTrue(bridge.FirstFrameSent, "firstFrameReady not sent");
            Assert.IsTrue(bridge.GameReadySent, "gameReady not sent once the garage is up");
        }

        [UnityTest]
        public IEnumerator Gas_DrivesForward()
        {
            _gm.ui.OnDrive();
            yield return null;
            Assert.AreEqual(GameState.Run, _gm.State);

            var v = _gm.Vehicle;
            float startX = v.chassis.position.x;

            PedalInput.SetGasHeld(true);
            yield return new WaitForSeconds(1.5f);
            PedalInput.SetGasHeld(false);

            float moved = v.chassis.position.x - startX;
            Debug.Log($"[Test] moved {moved:F1} m in 1.5 s, angle {v.chassis.rotation:F0}");
            Assert.Greater(moved, 3f, "gas should drive the Jeep to the right (check motorDirection)");
        }

        [UnityTest]
        public IEnumerator HoldingGas_EventuallyFlipsTheJeep()
        {
            // Design rule: flooring it is punished. Constant gas wheelies the Jeep over onto the driver's head.
            _gm.ui.OnDrive();
            yield return null;
            var v = _gm.Vehicle;
            float maxAngle = 0f;
            PedalInput.SetGasHeld(true);
            float t = 20f;
            while (!v.IsCrashed && t > 0f)
            {
                maxAngle = Mathf.Max(maxAngle, v.chassis.rotation);
                t -= Time.deltaTime;
                yield return null;
            }
            PedalInput.SetGasHeld(false);
            Debug.Log($"[Test] constant gas: crashed={v.IsCrashed} after {20f - t:F1} s, max nose-up {maxAngle:F0} deg");
            Assert.IsTrue(v.IsCrashed, "holding gas should wheelie the Jeep over");
        }

        [UnityTest]
        public IEnumerator FeatheredGas_MakesProgress()
        {
            // A simple "player" that eases off when the nose lifts. It should get well past where
            // constant gas flips, proving the hills are drivable with control.
            _gm.ui.OnDrive();
            yield return null;
            var v = _gm.Vehicle;
            float startX = v.chassis.position.x;
            var stage = _gm.stageConfig;
            float t = 25f;
            while (!v.IsCrashed && t > 0f)
            {
                float rot = Mathf.DeltaAngle(0f, v.chassis.rotation);
                float pitch = rot - stage.SlopeAt(v.chassis.position.x); // nose-up relative to the ground
                bool gas, brake = false;
                if (v.Grounded) gas = pitch < 12f;
                else { gas = rot < -5f; brake = rot > 25f; }
                PedalInput.SetGasHeld(gas);
                PedalInput.SetBrakeHeld(brake);
                t -= Time.deltaTime;
                yield return null;
            }
            PedalInput.ReleaseAll();
            float dist = v.chassis.position.x - startX;
            Debug.Log($"[Test] feathered gas: {dist:F0} m, crashed={v.IsCrashed}");
            Assert.Greater(dist, 80f, "a careful driver should get well past the first hills");
        }

        [UnityTest]
        public IEnumerator ReleasingGas_SlowsTheJeep()
        {
            _gm.ui.OnDrive();
            yield return null;
            var v = _gm.Vehicle;
            PedalInput.SetGasHeld(true);
            yield return new WaitForSeconds(1.2f);
            PedalInput.SetGasHeld(false);
            float vRelease = v.chassis.linearVelocity.magnitude;
            yield return new WaitForSeconds(3f);
            float vLater = v.chassis.linearVelocity.magnitude;
            Debug.Log($"[Test] coast: {vRelease:F1} m/s at release, {vLater:F1} m/s 3 s later");
            Assert.Less(vLater, vRelease * 0.5f, "with no pedal the Jeep should slow down, not keep driving");
        }

        [UnityTest]
        public IEnumerator Brake_WhenStopped_Reverses()
        {
            _gm.ui.OnDrive();
            yield return new WaitForSeconds(0.5f); // settle
            var v = _gm.Vehicle;
            float startX = v.chassis.position.x;

            PedalInput.SetBrakeHeld(true);
            yield return new WaitForSeconds(1.5f);
            PedalInput.SetBrakeHeld(false);

            Assert.Less(v.chassis.position.x, startX - 0.5f, "brake from standstill should reverse");
        }

        [UnityTest]
        public IEnumerator AirControl_GasRotatesNoseUp()
        {
            _gm.ui.OnDrive();
            yield return new WaitForSeconds(0.3f);
            var v = _gm.Vehicle;

            // Lift the Jeep high into the air and hold gas: it should rotate counter-clockwise (nose up).
            foreach (var rb in v.GetComponentsInChildren<Rigidbody2D>())
            {
                rb.position += new Vector2(0f, 30f);
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            yield return new WaitForFixedUpdate();
            float a0 = v.chassis.rotation;
            PedalInput.SetGasHeld(true);
            yield return new WaitForSeconds(0.5f);
            PedalInput.SetGasHeld(false);
            Assert.IsFalse(v.Grounded);
            Assert.Greater(v.chassis.rotation - a0, 10f, "gas in the air should rotate nose-up (CCW)");
        }
    }
}
