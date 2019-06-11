import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-user-component',
  templateUrl: './user.component.html'
})
export class UserComponent {
  public id: string;
  public user: User;
  public loaded: boolean;

  constructor(private a: ActivatedRoute, private http: HttpClient, @Inject('BASE_URL') private baseUrl: string) {
    this.loaded = false;
  }

  ngOnInit() {
    this.id = this.a.snapshot.params.userid;
    this.http.get<User>(this.baseUrl + 'api/Users/' + this.id).subscribe(result => {
      this.user = result;
      this.loaded = true;
    }, error => console.error(error));
  }
}

interface User {
  name: string;
  id: number;
  imageHash: string;
}
