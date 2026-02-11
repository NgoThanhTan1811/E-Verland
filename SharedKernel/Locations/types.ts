export interface Province {
  id: number;
  name: string;
}

export interface District {
  id: number;
  name: string;
  provinceId: number;
}

export interface Ward {
  id: number;
  name: string;
  districtId: number;
  provinceId: number;
}

export interface LocationCodes {
  provinceId?: number;
  districtId?: number;
  wardId?: number;
}
