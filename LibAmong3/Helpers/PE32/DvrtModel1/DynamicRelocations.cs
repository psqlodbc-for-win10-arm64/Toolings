using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LibAmong3.Helpers.PE32.DvrtModel1
{
    [XmlRoot]
    public class DynamicRelocations
    {
        [XmlElement] public Arm64X? Arm64X { get; set; }
    }
}
