import { Component, Inject } from '@angular/core';
import { HttpClient} from '@angular/common/http';
import { Router } from "@angular/router";
import { AuthService } from '../auth.service';
import { User } from '../auth.service';

@Component({
  selector: 'app-nav-menu',
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.css']
})
export class NavMenuComponent 
{
  public user: User;
  public loggedIn: boolean;

  constructor(
    private router: Router, 
    private http: HttpClient, 
    @Inject('BASE_URL') private baseUrl: string,
    private authService: AuthService) 
  {
    authService.registerLoginChangedCallback(() => this.onLoginChanged());
    authService.refresh();
    
  }

  onLoginChanged(): void
  {
    this.loggedIn = this.authService.isLoggedIn();

    if(this.loggedIn)
    {
      this.user = this.authService.getUser();
    }
  }

  onSignIn()
  {
    this.router.navigate(['/login']);
  }
}