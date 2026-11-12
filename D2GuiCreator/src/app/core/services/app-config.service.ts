import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

export interface AppBuildInfo {
  buildNumber: string;
  version: string;
  branch: string;
  buildDate: string;
  copyright: string;
  department: string;
}

@Injectable({
  providedIn: 'root'
})
export class AppConfigService {
  getBuildInfo(): AppBuildInfo {
    // Pobierz prawdziwe informacje z environment lub z globalnego obiektu window
    const buildInfo = (window as any).__BUILD_INFO__ || {};
    const now = new Date();
    const formattedDate = `${now.getDate().toString().padStart(2, '0')}.${(now.getMonth() + 1).toString().padStart(2, '0')}.${now.getFullYear()}, ${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}:${now.getSeconds().toString().padStart(2, '0')}`;
    
    return {
      buildNumber: buildInfo.buildNumber || '00000',
      version: buildInfo.gitHash || buildInfo.version || 'dev',
      branch: buildInfo.gitBranch || environment.name,
      buildDate: buildInfo.buildDate || formattedDate,
      copyright: '© 2019 - Doc2',
      department: 'Legal Operations Department'
    };
  }
}
