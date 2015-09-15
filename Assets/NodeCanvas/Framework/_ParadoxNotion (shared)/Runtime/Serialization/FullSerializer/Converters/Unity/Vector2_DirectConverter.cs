using System;
using System.Collections.Generic;

namespace ParadoxNotion.Serialization.FullSerializer {
    partial class fsConverterRegistrar {
        public static Internal.DirectConverters.Vector2_DirectConverter Register_Vector2_DirectConverter;
    }
}

namespace ParadoxNotion.Serialization.FullSerializer.Internal.DirectConverters {
    public class Vector2_DirectConverter : fsDirectConverter<UnityEngine.Vector2> {
        protected override fsResult DoSerialize(UnityEngine.Vector2 model, Dictionary<string, fsData> serialized) {
            var result = fsResult.Success;

            result += SerializeMember(serialized, "x", model.x);
            result += SerializeMember(serialized, "y", model.y);

            return result;
        }

        protected override fsResult DoDeserialize(Dictionary<string, fsData> data, ref UnityEngine.Vector2 model) {
            var result = fsResult.Success;

            var t0 = model.x;
            result += DeserializeMember(data, "x", out t0);
            model.x = t0;

            var t1 = model.y;
            result += DeserializeMember(data, "y", out t1);
            model.y = t1;

            return result;
        }

        public override object CreateInstance(fsData data, Type storageType) {
            return new UnityEngine.Vector2();
        }
    }
}
