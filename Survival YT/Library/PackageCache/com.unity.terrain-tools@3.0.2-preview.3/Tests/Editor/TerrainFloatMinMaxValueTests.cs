using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class TerrainFloatMinMaxValueTests
{
    private TerrainFloatMinMaxValue m_minMaxFloat;

    [SetUp]
    public void SetUp()
    {
        m_minMaxFloat = new TerrainFloatMinMaxValue(new GUIContent("test"), 5, 0,10);
    }
    
    [Test]
    public void MinMax()
    {
        // setting min value higher than current value sets current value to min
        m_minMaxFloat.minValue = 6;
        Assert.That(m_minMaxFloat.value, Is.EqualTo(6));
        
        // setting max less than value should modify value 
        m_minMaxFloat.minValue = 0;
        m_minMaxFloat.maxValue = 4;
        Assert.That(m_minMaxFloat.value, Is.EqualTo(4));
    }

    [Test]
    public void MinMaxChangePaired()
    {
        // setting min value higher than current value sets current value to min
        m_minMaxFloat.minValue = 11;
        Assert.That(m_minMaxFloat.value, Is.EqualTo(11));
        Assert.That(m_minMaxFloat.maxValue, Is.EqualTo(11));

        // setting min value higher than current value sets current value to min
        m_minMaxFloat.maxValue = 9;
        Assert.That(m_minMaxFloat.value, Is.EqualTo(9));
        Assert.That(m_minMaxFloat.minValue, Is.EqualTo(9));
    }
    
    [Test]
    public void MaxClamp()
    {
        // test clamping max
        m_minMaxFloat.shouldClampMax = true;
        m_minMaxFloat.maxClamp = 10;
        m_minMaxFloat.maxValue = 11;
        Assert.That(m_minMaxFloat.maxValue, Is.EqualTo(10));
        
        // clear clamp
        m_minMaxFloat.shouldClampMax = false;
        m_minMaxFloat.maxValue = 11;
        Assert.That(m_minMaxFloat.maxValue, Is.EqualTo(11));
    }
    
    [Test]
    public void MinClamp()
    {
        // test clamping max
        m_minMaxFloat.shouldClampMin = true;
        m_minMaxFloat.minClamp = 0;
        m_minMaxFloat.minValue = -1;
        Assert.That(m_minMaxFloat.minValue, Is.EqualTo(0));
        
        // clear clamp
        m_minMaxFloat.shouldClampMin = false;
        m_minMaxFloat.maxValue = -1;
        Assert.That(m_minMaxFloat.minValue, Is.EqualTo(-1));
    }
    
}
