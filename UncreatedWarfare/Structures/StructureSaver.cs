using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Structures
{
    public class StructureSaver : JSONSaver<Structure>
    {
        public List<Structure> ActiveStructures;
        public StructureSaver() : base(Data.StructureStorage + "structures.json") { }
        protected override string LoadDefaults() => "[]";
        public static void PlaceAllStructures()
        {

        }
    }

    public class Structure
    {
        public ushort id;
        public string state;
        public SerializableTransform transform;
        public ulong owner;
        public ulong group;
        [JsonConstructor]
        public Structure(ushort id, string state, SerializableTransform transform, ulong owner, ulong group)
        {
            this.id = id;
            this.state = state;
            this.transform = transform;
            this.owner = owner;
            this.group = group;
        }
    }
}
