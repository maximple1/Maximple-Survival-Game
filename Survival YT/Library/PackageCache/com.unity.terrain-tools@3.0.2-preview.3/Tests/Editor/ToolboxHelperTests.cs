using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    [TestFixture]
    public class ToolboxHelperTests
    {
        [Test]
        public void FlipTexture()
        {
            var texture = new Texture2D(2,2);
            texture.SetPixels(new []{Color.white, Color.black, Color.black, Color.black});
            texture.Apply();
            ToolboxHelper.FlipTexture(texture, true);
            var horizontalFlip = texture.GetPixels();
            Assert.That(horizontalFlip[1], Is.EqualTo(Color.white));

            ToolboxHelper.FlipTexture(texture, false);
            var verticalFlip = texture.GetPixels();
            Assert.That(verticalFlip[3], Is.EqualTo(Color.white));
        }
    }
}