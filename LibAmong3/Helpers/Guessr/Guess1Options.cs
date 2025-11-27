using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.Guessr
{
    public class Guess1Options
    {
        /// <summary>
        /// Disable detection of Arm64X binaries.
        /// </summary>
        public bool DisableArm64XDetection { get; set; }

        /// <summary>
        /// Look at CHPE metadata version to help determine the binary form.
        /// </summary>
        public bool LookAtCHPEMetadataVersion { get; set; }

        /// <summary>
        /// Look at DVRT header to help determine the binary form.
        /// </summary>
        public bool LookAtDvrtHeader { get; set; }
    }
}
