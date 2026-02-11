// libs/location/src/location.store.ts

import districtsJson from '../src/lib/districts.json';
import provincesJson from '../src/lib/provinces.json';
import wardsJson from '../src/lib/wards.json';
import { District, Province, Ward } from './types';

export class LocationStore {
  private readonly provinceById = new Map<number, Province>();
  private readonly districtById = new Map<number, District>();
  private readonly wardById = new Map<number, Ward>();

  private readonly districtsByProvince = new Map<number, District[]>();
  private readonly wardsByDistrict = new Map<number, Ward[]>();

  // Optional: expose counts for debug/health
  private readonly stats = {
    provinces: 0,
    districts: 0,
    wards: 0,
  };

  constructor() {
    console.log('[LocationStore] loading dataset into RAM...');

    const provinces = provincesJson as Province[];
    const districts = districtsJson as District[];
    const wards = wardsJson as Ward[];

    // --- Provinces ---
    for (const p of provinces) {
      this.provinceById.set(p.id, p);
    }

    // --- Districts ---
    for (const d of districts) {
      this.districtById.set(d.id, d);

      const arr = this.districtsByProvince.get(d.provinceId) ?? [];
      arr.push(d);
      this.districtsByProvince.set(d.provinceId, arr);
    }

    // --- Wards ---
    for (const w of wards) {
      this.wardById.set(w.id, w);

      const arr = this.wardsByDistrict.get(w.districtId) ?? [];
      arr.push(w);
      this.wardsByDistrict.set(w.districtId, arr);
    }

    this.stats.provinces = this.provinceById.size;
    this.stats.districts = this.districtById.size;
    this.stats.wards = this.wardById.size;

    console.log(
      `[LocationStore] ready (provinces=${this.stats.provinces}, districts=${this.stats.districts}, wards=${this.stats.wards})`
    );
  }

  /* =========================
   * Dropdown APIs
   * ========================= */

  getProvinces(): Province[] {
    return Array.from(this.provinceById.values());
  }

  getDistricts(provinceId: number): District[] {
    return this.districtsByProvince.get(provinceId) ?? [];
  }

  getWards(districtId: number): Ward[] {
    return this.wardsByDistrict.get(districtId) ?? [];
  }

  /* =========================
   * Lookup by id
   * ========================= */

  getProvince(provinceId: number): Province | null {
    return this.provinceById.get(provinceId) ?? null;
  }

  getDistrict(districtId: number): District | null {
    return this.districtById.get(districtId) ?? null;
  }

  getWard(wardId: number): Ward | null {
    return this.wardById.get(wardId) ?? null;
  }
}
