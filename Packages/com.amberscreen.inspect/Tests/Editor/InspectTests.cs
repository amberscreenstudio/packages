using NUnit.Framework;
using UnityEngine;

namespace AmberScreen.Inspect.Tests
{
    public sealed class InspectTests
    {
        GameObject m_Camera;
        GameObject m_Parent;
        GameObject m_Target;
        GameObject m_Child;
        InspectViewer m_Viewer;

        [SetUp]
        public void SetUp()
        {
            m_Camera = new GameObject("TestCamera", typeof(Camera));
            m_Camera.transform.SetPositionAndRotation(new Vector3(0f, 1.6f, -3f), Quaternion.Euler(10f, 5f, 0f));
            m_Viewer = m_Camera.AddComponent<InspectViewer>();
            m_Viewer.viewCamera = m_Camera.GetComponent<Camera>();
            m_Viewer.transitionSeconds = 0f;
            m_Viewer.focusSeconds = 0f;
            m_Viewer.handleInput = false;
            m_Viewer.inspectLayer = 5;

            m_Parent = new GameObject("Parent");
            m_Parent.transform.SetPositionAndRotation(new Vector3(1f, 2f, 3f), Quaternion.Euler(0f, 40f, 0f));
            new GameObject("Before").transform.SetParent(m_Parent.transform, false);

            m_Target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_Target.name = "Target";
            m_Target.transform.SetParent(m_Parent.transform, false);
            m_Target.transform.localPosition = new Vector3(0.2f, 0.5f, -0.1f);
            m_Target.transform.localRotation = Quaternion.Euler(10f, 20f, 30f);
            m_Target.transform.localScale = new Vector3(0.3f, 0.2f, 0.4f);
            m_Target.AddComponent<Rigidbody>().isKinematic = false;

            new GameObject("After").transform.SetParent(m_Parent.transform, false);

            m_Child = new GameObject("Child");
            m_Child.transform.SetParent(m_Target.transform, false);
            m_Child.layer = 2;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Parent);
            Object.DestroyImmediate(m_Target);
            Object.DestroyImmediate(m_Camera);
        }

        [Test]
        public void EndInspectRestoresParentPoseLayersAndPhysics()
        {
            var t = m_Target.transform;
            int sibling = t.GetSiblingIndex();
            Vector3 lp = t.localPosition;
            Quaternion lr = t.localRotation;
            Vector3 ls = t.localScale;
            int controlsOff = 0, controlsOn = 0;
            m_Viewer.PlayerControlsCallback = on => { if (on) controlsOn++; else controlsOff++; };

            Assert.That(m_Viewer.Inspect(t), Is.True);
            Assert.That(m_Viewer.IsInspecting, Is.True);
            Assert.That(t.parent, Is.EqualTo(m_Viewer.Pivot));
            Assert.That(m_Target.layer, Is.EqualTo(5));
            Assert.That(m_Child.layer, Is.EqualTo(5));
            Assert.That(m_Target.GetComponent<Rigidbody>().isKinematic, Is.True);
            Assert.That(m_Target.GetComponent<Collider>().enabled, Is.False);
            Assert.That(controlsOff, Is.EqualTo(1));

            m_Viewer.Rotate(new Vector2(45f, 20f));
            m_Viewer.Zoom(0.3f);
            m_Viewer.EndInspect();

            Assert.That(m_Viewer.IsInspecting, Is.False);
            Assert.That(t.parent, Is.EqualTo(m_Parent.transform));
            Assert.That(t.GetSiblingIndex(), Is.EqualTo(sibling));
            Assert.That(Vector3.Distance(t.localPosition, lp), Is.LessThan(1e-5f));
            Assert.That(Quaternion.Angle(t.localRotation, lr), Is.LessThan(0.01f));
            Assert.That(Vector3.Distance(t.localScale, ls), Is.LessThan(1e-5f));
            Assert.That(m_Target.layer, Is.EqualTo(0));
            Assert.That(m_Child.layer, Is.EqualTo(2));
            Assert.That(m_Target.GetComponent<Rigidbody>().isKinematic, Is.False);
            Assert.That(m_Target.GetComponent<Collider>().enabled, Is.True);
            Assert.That(controlsOn, Is.EqualTo(1));
        }

        [Test]
        public void HeldObjectSitsAtHoldDistanceInFrontOfCamera()
        {
            m_Viewer.holdDistance = 0.8f;
            m_Viewer.Inspect(m_Target.transform);
            var local = m_Camera.transform.InverseTransformPoint(m_Viewer.Pivot.position);
            Assert.That(local.x, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(local.y, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(local.z, Is.EqualTo(0.8f).Within(1e-4f));
        }

        [Test]
        public void ZoomIsClampedBetweenMinAndMax()
        {
            m_Viewer.minDistance = 0.3f;
            m_Viewer.maxDistance = 1.2f;
            m_Viewer.holdDistance = 5f;
            m_Viewer.Inspect(m_Target.transform);
            Assert.That(m_Viewer.Distance, Is.EqualTo(1.2f).Within(1e-5f), "hold distance clamped");
            m_Viewer.Zoom(-10f);
            Assert.That(m_Viewer.Distance, Is.EqualTo(0.3f).Within(1e-5f));
            m_Viewer.Zoom(0.2f);
            Assert.That(m_Viewer.Distance, Is.EqualTo(0.5f).Within(1e-5f));
            m_Viewer.Zoom(10f);
            Assert.That(m_Viewer.Distance, Is.EqualTo(1.2f).Within(1e-5f));
        }

        [TestCase(0.5f, 0.3f, 1.2f, 0.5f)]
        [TestCase(0.1f, 0.3f, 1.2f, 0.3f)]
        [TestCase(9f, 0.3f, 1.2f, 1.2f)]
        [TestCase(0.5f, 1.2f, 0.3f, 0.5f)] // min/max swapped
        public void ClampDistance(float d, float min, float max, float expected) =>
            Assert.That(InspectMath.ClampDistance(d, min, max), Is.EqualTo(expected).Within(1e-5f));

        [Test]
        public void FocusTurnsSpotTowardCameraAndSendsLabel()
        {
            var spotGo = new GameObject("Spot");
            spotGo.transform.SetParent(m_Target.transform, false);
            spotGo.transform.localPosition = new Vector3(0.5f, 0f, 0f);
            var spot = spotGo.AddComponent<InspectHotspot>();
            spot.label = "Serial";
            string label = null;
            m_Viewer.onHotspotLabel.AddListener(l => label = l);

            m_Viewer.Inspect(m_Target.transform);
            m_Viewer.NextHotspot();

            Assert.That(label, Is.EqualTo("Serial"));
            Assert.That(m_Viewer.FocusedHotspot, Is.EqualTo(spot));
            Vector3 toSpot = m_Camera.transform.InverseTransformDirection(spotGo.transform.position - m_Viewer.Pivot.position);
            Assert.That(Vector3.Angle(toSpot, Vector3.back), Is.LessThan(0.5f));

            m_Viewer.EndInspect();
            Assert.That(label, Is.EqualTo(""));
        }

        [TestCase(-1, 1, 3, 0)]
        [TestCase(-1, -1, 3, 2)]
        [TestCase(2, 1, 3, 0)]
        [TestCase(0, -1, 3, 2)]
        [TestCase(0, 1, 0, -1)]
        public void StepWraps(int index, int dir, int count, int expected) =>
            Assert.That(InspectMath.Step(index, dir, count), Is.EqualTo(expected));
    }
}
