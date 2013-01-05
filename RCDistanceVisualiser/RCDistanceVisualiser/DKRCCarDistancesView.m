//
//  DKRCCarDistancesView.m
//  RCDistanceVisualiser
//
//  Created by Daniel Kennett on 05/01/2013.
//  Copyright (c) 2013 Daniel Kennett. All rights reserved.
//

#import "DKRCCarDistancesView.h"

@implementation DKRCCarDistancesView

-(id)initWithFrame:(NSRect)frameRect {
	self = [super initWithFrame:frameRect];
	if (self) {
		self.maximumDrawDistance = 350;
	}
	return self;
}

-(void)drawRect:(NSRect)dirtyRect {
    // Drawing code here.

	// Here is the least efficient drawing code you'll ever see. Enjoy!

	NSRect bounds = self.bounds;
	[[NSColor whiteColor] set];
	NSRectFill(bounds);

	NSImage *carImage = [NSImage imageNamed:@"car"];
	NSSize carImageSize = carImage.size;
	carImageSize.width = carImageSize.width / 2.0;
	carImageSize.height = carImageSize.height / 2.0;

	NSRect carImageRect = NSMakeRect(NSMidX(bounds) - (carImageSize.width / 2),
									 NSMidY(bounds) - (carImageSize.height / 2),
									 carImageSize.width,
									 carImageSize.height);

	// Make sure we're drawing at pixel bounds.
	carImageRect.origin.x = floor(carImageRect.origin.x);
	carImageRect.origin.y = floor(carImageRect.origin.y);

	// Draw sensor lines
	[[NSColor darkGrayColor] set];
	CGFloat dash[2];
	dash[0] = 5.0;
	dash[1] = 5.0;

	NSBezierPath *frontAndRearPath = [NSBezierPath bezierPath];
	[frontAndRearPath moveToPoint:NSMakePoint(NSMinX(bounds), NSMidY(carImageRect))];
	[frontAndRearPath lineToPoint:NSMakePoint(NSMaxX(bounds), NSMidY(carImageRect))];
	[frontAndRearPath setLineDash:(const CGFloat *)&dash count:2 phase:0.0];
	[frontAndRearPath stroke];

	// Front corners are around +- 30° front straight.
	CGFloat lineDegrees = 30.0;
	NSPoint frontLeftStart = NSMakePoint(NSMaxX(carImageRect) - 15, NSMaxY(carImageRect) - (NSHeight(carImageRect) / 5));
	CGFloat angledLineLength = NSMinX(carImageRect) * 2.0;
	NSPoint frontLeftEnd = NSMakePoint(frontLeftStart.x + (angledLineLength * sin((90.0 - lineDegrees) * (M_PI / 180.0))),
									   frontLeftStart.y + (angledLineLength * cos((90.0 - lineDegrees) * (M_PI / 180.0))));

	NSBezierPath *frontLeftPath = [NSBezierPath bezierPath];
	[frontLeftPath moveToPoint:frontLeftStart];
	[frontLeftPath lineToPoint:frontLeftEnd];
	[frontLeftPath setLineDash:(const CGFloat *)&dash count:2 phase:0.0];
	[frontLeftPath stroke];

	NSPoint frontRightStart = NSMakePoint(NSMaxX(carImageRect) - 15, NSMinY(carImageRect) + (NSHeight(carImageRect) / 5));
	NSPoint frontRightEnd = NSMakePoint(frontRightStart.x + (angledLineLength * sin((90.0 + lineDegrees) * (M_PI / 180.0))),
										frontRightStart.y + (angledLineLength * cos((90.0 + lineDegrees) * (M_PI / 180.0))));

	NSBezierPath *frontRightPath = [NSBezierPath bezierPath];
	[frontRightPath moveToPoint:frontRightStart];
	[frontRightPath lineToPoint:frontRightEnd];
	[frontRightPath setLineDash:(const CGFloat *)&dash count:2 phase:0.0];
	[frontRightPath stroke];

	[carImage drawInRect:carImageRect
				fromRect:NSZeroRect
			   operation:NSCompositeSourceOver
				fraction:1.0];

	CGFloat pixelsPerDistanceUnit = CGRectGetMinX(carImageRect) / self.maximumDrawDistance;
	[[[NSColor darkGrayColor] colorWithAlphaComponent:0.8] set];

	// Rear distance
	if (self.rearDistance > 0) {
		CGFloat rearX = CGRectGetMinX(carImageRect) - (pixelsPerDistanceUnit * self.rearDistance);
		rearX = floor(rearX);
		NSRect rearRect = NSMakeRect(NSMinX(bounds), NSMinY(bounds), rearX, NSHeight(bounds));
		[[NSBezierPath bezierPathWithRect:rearRect] fill];
	}

	// Front distance
	if (self.frontLeftDistance > 0 || self.frontMiddleDistance > 0 || self.frontRightDistance > 0) {
		CGFloat frontLeftDistance = (self.frontLeftDistance > 0 ? self.frontLeftDistance : self.maximumDrawDistance) * pixelsPerDistanceUnit;
		CGFloat frontMiddleDistance = (self.frontMiddleDistance > 0 ? self.frontMiddleDistance : self.maximumDrawDistance) * pixelsPerDistanceUnit;
		CGFloat frontRightDistance = (self.frontRightDistance > 0 ? self.frontRightDistance : self.maximumDrawDistance) * pixelsPerDistanceUnit;

		NSPoint frontLeftPoint = NSMakePoint(frontLeftStart.x + (frontLeftDistance * sin((90.0 - lineDegrees) * (M_PI / 180.0))),
											 frontLeftStart.y + (frontLeftDistance * cos((90.0 - lineDegrees) * (M_PI / 180.0))));

		NSPoint frontRightPoint = NSMakePoint(frontRightStart.x + (frontRightDistance * sin((90.0 + lineDegrees) * (M_PI / 180.0))),
											  frontRightStart.y + (frontRightDistance * cos((90.0 + lineDegrees) * (M_PI / 180.0))));

		NSPoint frontMiddlePoint = NSMakePoint(CGRectGetMaxX(carImageRect) + frontMiddleDistance,
											   NSMidY(bounds));

		NSBezierPath *frontPath = [NSBezierPath bezierPath];
		[frontPath moveToPoint:NSMakePoint(frontLeftPoint.x - ((frontMiddlePoint.x - frontLeftPoint.x) / 2), NSMaxY(bounds))];
		[frontPath lineToPoint:frontLeftPoint];
		[frontPath lineToPoint:frontMiddlePoint];
		[frontPath lineToPoint:frontRightPoint];
		[frontPath lineToPoint:NSMakePoint(frontRightPoint.x - ((frontMiddlePoint.x - frontRightPoint.x) / 2), NSMinY(bounds))];

		[frontPath lineToPoint:NSMakePoint(NSMaxX(bounds), NSMinY(bounds))];
		[frontPath lineToPoint:NSMakePoint(NSMaxX(bounds), NSMaxY(bounds))];
		[frontPath closePath];
		[frontPath fill];
	}
}

@end
