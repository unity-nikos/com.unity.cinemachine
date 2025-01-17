using NUnit.Framework;
using Unity.Cinemachine.Editor;
using System.IO;

namespace Unity.Cinemachine.Tests.Editor
{
    [TestFixture]
    public class ScriptableObjectUtilityTests
    {
        [Test]
        public void CinemachineInstallPathIsValid()
        {
            var pathToCmLogo = Path.Combine(ScriptableObjectUtility.kPackageRoot + 
                "/Editor/EditorResources/Icons/CmCamera@256.png");
            Assert.That(File.Exists(pathToCmLogo));
        }
        
        [Test]
        public void CinemachineInstallRelativePathIsValid()
        {
            var relativePathToCmLogo = Path.Combine(ScriptableObjectUtility.kPackageRoot + 
                "/Editor/EditorResources/Icons/CmCamera@256.png");
            var pathToCmLogo = Path.GetFullPath(relativePathToCmLogo);
            Assert.That(File.Exists(pathToCmLogo));
        }
    }
}