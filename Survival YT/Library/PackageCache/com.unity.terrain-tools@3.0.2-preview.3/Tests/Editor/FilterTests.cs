using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UObject = UnityEngine.Object;

namespace UnityEditor.Experimental.TerrainAPI
{
    [TestFixture(Category = "Filters")]
    public class FilterTests : BaseTests
    {
        private FilterContext m_Context;
        private FilterStack m_Stack;

        public override void Setup()
        {
            base.Setup();

            m_Stack = ScriptableObject.CreateInstance<FilterStack>();
            m_Context = new FilterContext(FilterUtility.defaultFormat, Vector3.zero, 1f, 0f);
        }

        public override void Teardown()
        {
            m_Stack.Clear(true);
            UObject.DestroyImmediate(m_Stack);

            base.Teardown();
        }

        [Test]
        public void Add_Filter()
        {
            // setup
            float addValue = 9000f;
            var addFilter = FilterUtility.CreateInstance<AddFilter>();
            addFilter.value = addValue;

            var prevRT = RenderTexture.active;
            var src = RTUtils.GetTempHandle(RTUtils.GetDescriptor(1, 1, 0, GraphicsFormat.R16G16B16A16_SFloat, 0, false));
            var dest = RTUtils.GetTempHandle(RTUtils.GetDescriptor(1, 1, 0, GraphicsFormat.R16G16B16A16_SFloat, 0, false));
            Graphics.Blit(Texture2D.blackTexture, src);
            Graphics.Blit(Texture2D.blackTexture, dest);

            // eval
            addFilter.Eval(m_Context, src, dest);

            var tex = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
            RenderTexture.active = dest;
            tex.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);

            var check = tex.GetPixel(0, 0).r;

            // clean up
            RenderTexture.active = prevRT;
            UObject.DestroyImmediate(tex);
            RTUtils.Release(src);
            RTUtils.Release(dest);
            UObject.DestroyImmediate(addFilter);

            Assert.That(check, Is.EqualTo(addValue));
        }

        [Test]
        public void Values_Can_Be_Negative()
        {
            // setup
            float addValue = -10;
            var addFilter = FilterUtility.CreateInstance<AddFilter>();
            addFilter.value = addValue;
            m_Stack.Add(addFilter);

            var prevRT = RenderTexture.active;
            var dest = RTUtils.GetTempHandle(RTUtils.GetDescriptor(1, 1, 0, GraphicsFormat.R16G16B16A16_SFloat, 0, false));
            Graphics.Blit(Texture2D.blackTexture, dest); // init to black

            // eval
            m_Stack.Eval(m_Context, null, dest); // source isn't actually used yet

            var tex = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
            RenderTexture.active = dest;
            tex.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);

            var check = tex.GetPixel(0, 0).r - 1; // minus 1 because we start off with a white texture within FilterStack.Eval

            // clean up
            RenderTexture.active = prevRT;
            UObject.DestroyImmediate(tex);
            RTUtils.Release(dest);

            Assert.That(check, Is.EqualTo(addValue));
        }

        [Test]
        public void Values_Can_Be_Greater_Than_One()
        {
            // setup
            float addValue = 10;
            var addFilter = FilterUtility.CreateInstance<AddFilter>();
            addFilter.value = addValue;
            m_Stack.Add(addFilter);

            var prevRT = RenderTexture.active;
            var dest = RTUtils.GetTempHandle(RTUtils.GetDescriptor(1, 1, 0, GraphicsFormat.R16G16B16A16_SFloat, 0, false));
            Graphics.Blit(Texture2D.blackTexture, dest); // init to black

            // eval
            m_Stack.Eval(m_Context, null, dest); // source isn't actually used yet

            var tex = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
            RenderTexture.active = dest;
            tex.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);

            var check = tex.GetPixel(0, 0).r - 1; // minus 1 because we start off with a white texture within FilterStack.Eval

            // clean up
            RenderTexture.active = prevRT;
            UObject.DestroyImmediate(tex);
            RTUtils.Release(dest);

            Assert.That(check, Is.EqualTo(addValue));
        }
    }
} 