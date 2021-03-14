using NUnit.Framework;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class BaseTests
    {
        // private ulong m_PrevTextureMemory; // TODO
        private int m_PrevRTHandlesCount;

        [SetUp]
        public virtual void Setup()
        {
            // m_PrevTextureMemory = Texture.totalTextureMemory;
            m_PrevRTHandlesCount = RTUtils.GetHandleCount();
        }

        [TearDown]
        public virtual void Teardown()
        {
            // check Texture memory and RTHandle count
            // var currentTextureMemory = Texture.totalTextureMemory;
            // Assert.True(m_PrevTextureMemory == currentTextureMemory, $"Texture memory leak. Was {m_PrevTextureMemory} but is now {currentTextureMemory}. Diff = {currentTextureMemory - m_PrevTextureMemory}");
            var currentRTHandlesCount = RTUtils.GetHandleCount();
            Assert.True(m_PrevRTHandlesCount == RTUtils.GetHandleCount(), $"RTHandle leak. Was {m_PrevRTHandlesCount} but is now {currentRTHandlesCount}. Diff = {currentRTHandlesCount - m_PrevRTHandlesCount}");
        }
    }
}
