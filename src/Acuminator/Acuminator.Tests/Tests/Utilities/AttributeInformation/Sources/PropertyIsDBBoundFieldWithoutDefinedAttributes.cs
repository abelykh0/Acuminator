﻿using PX.Data;
namespace PX.Objects.HackathonDemo
{
    public class IIGPOALCLandedCost : IBqlTable
    {
        #region FieldUnbound1
        public abstract class selected : PX.Data.IBqlField { }
        protected bool? _Unbound1;
        [PeriodID(IsDBField = false)]
        [PXDefault(false)]
        [PXUIField(DisplayName = "Unbound 1")]
        public virtual bool? Unbound1
        {
            get
            {
                return _Unbound1;
            }
            set
            {
                _Unbound1 = value;
            }
        }
        #endregion
        #region FieldUnbound2
        public abstract class selected : PX.Data.IBqlField { }
        protected bool? _Unbound2;
        [AcctSub(IsDBField = false)]
        [PXDefault(false)]
        [PXUIField(DisplayName = "Unbound 2")]
        public virtual bool? Unbound2
        {
            get
            {
                return _Unbound2;
            }
            set
            {
                _Unbound2 = value;
            }
        }
        #endregion
        #region FieldBound1
        public abstract class cost : PX.Data.IBqlField { }
        protected decimal? _Bound1;
        [PeriodID(IsDBField = true)]
        [PXDefault]
        [PXUIField(DisplayName = "Bound 1")]
        public virtual decimal? Bound1 { get; set; }
        #endregion

        #region FieldBound2
        public abstract class cost : PX.Data.IBqlField { }
        protected decimal? _Bound2;
        [AcctSub(IsDBField = true)]
        [PXDefault]
        [PXUIField(DisplayName = "Bound 2")]
        public virtual decimal? Bound2 { get; set; }
        #endregion
    }
}