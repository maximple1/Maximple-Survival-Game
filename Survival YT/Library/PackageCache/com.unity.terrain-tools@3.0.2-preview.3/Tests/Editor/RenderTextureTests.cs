using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.TerrainAPI
{
    [TestFixture(Category = "RTUtils")]
    public class RenderTextureTests
    {
        RenderTextureDescriptor descriptor => RTUtils.GetDescriptor(512, 512, 0, GraphicsFormat.R16_UNorm);
        RenderTextureDescriptor descriptorRW => RTUtils.GetDescriptorRW(512, 512, 0, GraphicsFormat.R16_UNorm);
        private ulong m_PrevTextureMemory;
        private int m_PrevRTHandleCount;
        
        [SetUp]
        public void Setup()
        {
            m_PrevTextureMemory = Texture.totalTextureMemory;
            m_PrevRTHandleCount = RTUtils.GetHandleCount();
        }

        [TearDown]
        public void Cleanup()
        {
            Assert.True(m_PrevTextureMemory == Texture.totalTextureMemory, "Test was torn down with leaked texture memory");
            Assert.True(RTUtils.GetHandleCount() == m_PrevRTHandleCount, "Test was torn down with leaked RTHandle memory");
        }

        [Test]
        public void Descriptor_Is_Correct()
        {
            var desc = descriptor;
            
            Assert.True(desc.width == 512);
            Assert.True(desc.height == 512);
            Assert.True(desc.graphicsFormat == GraphicsFormat.R16_UNorm);
            Assert.True(desc.sRGB == false);
            Assert.True(desc.mipCount == 0);
            Assert.True(desc.useMipMap == false);
        }

        [Test]
        public void RW_Descriptor_Is_Correct()
        {
            var desc = descriptorRW;
            
            Assert.True(desc.width == 512);
            Assert.True(desc.height == 512);
            Assert.True(desc.graphicsFormat == GraphicsFormat.R16_UNorm);
            Assert.True(desc.sRGB == false);
            Assert.True(desc.mipCount == 0);
            Assert.True(desc.useMipMap == false);
            Assert.True(desc.enableRandomWrite == true);
        }

        [Test]
        public void RTHandle_Temporary_Is_Not_Null()
        {
            var handle = RTUtils.GetTempHandle(descriptor);
            Assert.True(handle != null);
            Assert.True(handle.RT != null);
            RTUtils.Release(handle);
        }

        [Test]
        public void RTHandleRW_Temporary_Is_Not_Null()
        {
            var handle = RTUtils.GetTempHandle(descriptorRW);
            Assert.True(handle != null);
            Assert.True(handle.RT != null);
            RTUtils.Release(handle);
        }

        [Test]
        public void RTHandleRW_Has_RW_Flag_Set()
        {
            var handle = RTUtils.GetTempHandle(descriptorRW);
            Assert.True(handle.RT.enableRandomWrite);
            RTUtils.Release(handle);
        }

        [Test]
        public void RTHandle_Scope_Cleanup()
        {
            var handle = RTUtils.GetTempHandle(descriptorRW);
            
            Assert.True(m_PrevRTHandleCount + 1 == RTUtils.GetHandleCount());
            
            using (handle.Scoped())
            {
                // keep empty
            }
            
            Assert.True(m_PrevRTHandleCount == RTUtils.GetHandleCount());
        }

        [Test]
        public void RTHandle_Cleanup_Throws_Exception_On_Non_RTHandle_RenderTexture()
        {
            var handle = new RTHandle();
            handle.SetRenderTexture(RenderTexture.GetTemporary(descriptor), true);
            var pass = false;
            
            try
            {
                RTUtils.Release(handle);
            }
            catch (InvalidOperationException e)
            {
                pass = String.Compare(e.Message, "Attemping to release a RTHandle that is not currently tracked by the system. This should never happen",
                           StringComparison.Ordinal) == 0;
            }
            
            // manual cleanup for test purposes
            RenderTexture.ReleaseTemporary(handle.RT);
            
            Assert.True(pass);
        }

        [Test]
        public void RTHandle_Count_Is_Correct()
        {
            var handles = new RTHandle[10];
            for (int i = 0; i < handles.Length; ++i)
            {
                handles[i] = RTUtils.GetTempHandle(descriptor);
            }
            
            Assert.True(RTUtils.GetHandleCount() == m_PrevRTHandleCount + handles.Length);

            for (int i = 0; i < handles.Length; ++i)
            {
                RTUtils.Release(handles[i]);
            }
        }

        [Test]
        public void RTHandle_Release_Temporary()
        {
            var handle = RTUtils.GetTempHandle(descriptor);
            Assert.True(RTUtils.GetHandleCount() == m_PrevRTHandleCount + 1);
            RTUtils.Release(handle);
            Assert.True(RTUtils.GetHandleCount() == m_PrevRTHandleCount);
        }
        
        [Test]
        public void RTHandle_Release_New()
        {
            var handle = RTUtils.GetNewHandle(descriptor);
            Assert.True(RTUtils.GetHandleCount() == m_PrevRTHandleCount + 1);
            RTUtils.Release(handle);
            Assert.True(RTUtils.GetHandleCount() == m_PrevRTHandleCount);
        }
    }
}
