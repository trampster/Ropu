import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-fetch-data',
  templateUrl: './fetch-data.component.html'
})
export class FetchDataComponent {
  public groups: Group[];

  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    http.get<Group[]>(baseUrl + 'api/Groups/Groups').subscribe(result => {
      this.groups = result;
    }, error => console.error(error));
  }
}

interface Group {
  name: string;
  id: number;
}
